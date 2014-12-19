using BeatlesBlog.SimConnect;
using iniLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace AICarriers {
    class SimConnectHelpers {
        public static bool IsLocalRunning {
            get { return LookupDefaultPortNumber("SimConnect_Port_IPv4") != 0 || LookupDefaultPortNumber("SimConnect_Port_IPv6") != 0; }
        }

        public static int LookupDefaultPortNumber(string strValueName) {
            string[] simulators = {
                                      @"HKEY_CURRENT_USER\Software\Microsoft\Microsoft Games\Flight Simulator",
                                      @"HKEY_CURRENT_USER\Software\Microsoft\Microsoft ESP",
                                      @"HKEY_CURRENT_USER\Software\LockheedMartin\Prepar3D",
                                      @"HKEY_CURRENT_USER\Software\Lockheed Martin\Prepar3D v2",
                                      @"HKEY_CURRENT_USER\Software\Microsoft\Microsoft Games\Flight Simulator - Steam Edition"
                                  };
            foreach (string sim in simulators) {
                string value = (string)Microsoft.Win32.Registry.GetValue(sim, strValueName, null);
                if (!string.IsNullOrEmpty(value)) {
                    int port = int.Parse(value);
                    if (port != 0) { return port; }
                }
            }
            return 0;
        }
    }

    public class OpenEventArgs : EventArgs {
        public string SimulatorName { get; private set; }
        public OpenEventArgs(string SimulatorName) {
            this.SimulatorName = SimulatorName;
        }
    }

    class AICarriersManager {
        private static readonly string[] POSITION_TYPES = {
        		"User position",
        		"1 nm ahead",
        		"5 nm ahead",
        		"10 nm ahead",
        		"Facility waypoint",
        		"Return",
        	};

        private static readonly string[] MOVE_TYPES = {
        		"Forward",				    // 1
        		"Full stop",			    // 2
        		"Turn left 10 degrees",	    // 3
        		"Turn right 10 degrees",    // 4		
        		"Turn left 90 degrees",     // 5
        		"Turn right 90 degrees",    // 6
        		"Move to waypoint",		    // 7
        		"Delete",	 			    // 8
        		"Close window"			    // 9
        	};

        public static readonly string MENU_TITLE = "AI Ships";

        // Machine states
        enum MenuState {
            FORMATION_TYPE,
            POSITION_TYPE,
            SELECT_WAYPOINT,
            MOVE,
            SELECT_DESTINATION_WAYPOINT,
        }

        private Ini conf;

        // connection data
        private SimConnect sc;

        // current formation data
        private Formation currentFormation;
        private Unit[] units;
        private double currentHeading;
        private SIMCONNECT_DATA_FACILITY_WAYPOINT destWpt;

        // menu pointers
        private MenuState state = MenuState.FORMATION_TYPE;
        private int currentlyDisplayed = -1;
        private FormationList formList;
        private WaypointList wpList;

        // user position
        private UserTracker userTracker;

        // logging
        private Log log;

        // events
        public EventHandler<OpenEventArgs> OpenEvent;
        public EventHandler DisconnectEvent;

        protected virtual void OnRaiseOpenEvent(OpenEventArgs e) {
            EventHandler<OpenEventArgs> handler = OpenEvent;
            if (handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnRaiseDisconnectEvent(EventArgs e) {
            EventHandler handler = DisconnectEvent;
            if (handler != null) {
                handler(this, e);
            }
        }

        public AICarriersManager(string confDirectory) {
            log = Log.Instance;
            sc = new SimConnect(null);

            sc.OnRecvOpen += sc_OnRecvOpen;
            sc.OnRecvException += sc_OnRecvException;
            sc.OnRecvQuit += sc_OnRecvQuit;

            sc.OnRecvEvent += sc_OnRecvEvent;
            sc.OnRecvAssignedObjectId += sc_OnRecvAssignedObjectId;
            sc.OnRecvEventObjectAddremove += sc_OnRecvEventObjectAddremove;
            sc.OnRecvSimobjectData += sc_OnRecvSimobjectData;

            conf = new Ini(Path.Combine(confDirectory, "aicarriers.cfg"));
            formList = new FormationList(confDirectory, conf);

            initConnection();
            initErrorTracking();

            userTracker = new UserTracker(sc);
            wpList = new WaypointList(userTracker, sc);
        }

        public void initConnection() {
            if (SimConnectHelpers.IsLocalRunning) {
                try {
                    log.Info("Opening SimConnect connection.");
                    sc.Open(MENU_TITLE);
                }
                catch (SimConnect.SimConnectException ex) {
                    log.Warning(string.Format("Local connection failed.\r\n{0}", ex.ToString()));
                    try {
                        bool ipv6support = System.Net.Sockets.Socket.OSSupportsIPv6;
                        log.Info("Opening SimConnect connection " + (ipv6support ? "(IPv6)." : "(IPv4)."));
                        int scPort = ipv6support ? SimConnectHelpers.LookupDefaultPortNumber("SimConnect_Port_IPv6") : SimConnectHelpers.LookupDefaultPortNumber("SimConnect_Port_IPv4");
                        if (scPort == 0) { throw new SimConnect.SimConnectException("Invalid port."); }
                        sc.Open(MENU_TITLE, null, scPort, ipv6support);
                    }
                    catch (SimConnect.SimConnectException innerEx) {
                        log.Error(string.Format("Local connection failed.\r\n{0}", innerEx.ToString()));
                    }
                }
            }
            else {
                log.Warning("Flight Simulator must be running in order to connect to SimConnect.");
            }
        }

        public void Disconnect() {
            log.Info("Disconnecting.");
            sc.Close();
            OnRaiseDisconnectEvent(EventArgs.Empty);
        }

        /* **************************************************************
         * 
         * Simconnect handlers
         * 
         * **************************************************************
         */

        void sc_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data) {
            try {
                // move ai using throttle (forward/stop) or desired heading
                sc.MapClientEventToSimEvent(ID.EVENT_AI_THROTTLE, "THROTTLE_SET");

                // system events
                sc.SubscribeToSystemEvent(ID.EVENT_SIMSTART, "Sim");
                sc.SubscribeToSystemEvent(ID.EVENT_OBJECT_REMOVED, "ObjectRemoved");

                // shortcut key event
                string keyShort = conf.GetKeyValue("base", "menu_shortcut");
                if (string.IsNullOrEmpty(keyShort)) {
                    keyShort = "Shift+J";
                }

                sc.MapClientEventToSimEvent(ID.EVENT_CALL_MENU);
                sc.AddClientEventToNotificationGroup(ID.GROUP_KEYS, ID.EVENT_CALL_MENU, true);
                sc.SetNotificationGroupPriority(ID.GROUP_KEYS, (uint)SIMCONNECT_GROUP_PRIORITY.HIGHEST);
                sc.MapInputEventToClientEvent(ID.INPUT_KEYS, keyShort, ID.EVENT_CALL_MENU);
                sc.SetInputGroupState(ID.INPUT_KEYS, (uint)SIMCONNECT_STATE.ON);

                // init menu
                sc.MenuAddItem("AI Ships", ID.EVENT_TITLE_MENU, 0);
                sc.MenuAddSubItem(ID.EVENT_TITLE_MENU, "Show/hide menu (" + keyShort + ")", ID.EVENT_TITLE_MENU_SHOW, 0);

                // fire event
                OnRaiseOpenEvent(new OpenEventArgs(data.szApplicationName));
            }
            catch (SimConnect.SimConnectException ex) {
                log.Warning(ex.ToString());
            }
        }

        void sc_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data) {
            switch ((ID)data.uEventID) {
                case ID.EVENT_SIMSTART:
                    // sim start
                    // showMenu();		// just for debugging
                    break;
                case ID.EVENT_CALL_MENU:
                    // keyboard shortcut
                    showOrHideMenu();
                    break;
                case ID.EVENT_TITLE_MENU_SHOW:
                    showOrHideMenu();		// show main menu
                    break;
                case ID.EVENT_MENU_SELECT_FORMATION:
                    //
                    // formation select mode
                    //
                    SIMCONNECT_TEXT_RESULT trFormation = (SIMCONNECT_TEXT_RESULT)data.dwData;
                    menuDisplayed(trFormation, (int)data.uEventID);
                    currentFormation = formList.processEvent(sc, trFormation);
                    if (currentFormation != null && currentFormation.size() > 0) {
                        // next state
                        changeState(MenuState.POSITION_TYPE);
                        showMenu();
                    }
                    break;
                case ID.EVENT_MENU_SELECT_POSITION_TYPE:
                    //
                    // position type
                    // 
                    SIMCONNECT_TEXT_RESULT trType = (SIMCONNECT_TEXT_RESULT)data.dwData;
                    menuDisplayed(trType, (int)data.uEventID);

                    processPositionMenuEntry(trType);
                    break;
                case ID.EVENT_MENU_SELECT_WAYPOINT:
                    //
                    // waypoint select mode 
                    //
                    SIMCONNECT_TEXT_RESULT trWaypoint = (SIMCONNECT_TEXT_RESULT)data.dwData;
                    menuDisplayed(trWaypoint, (int)data.uEventID);

                    SIMCONNECT_DATA_FACILITY_WAYPOINT fw = wpList.processEvent(sc, trWaypoint);
                    if (fw != null) {
                        if (state == MenuState.SELECT_WAYPOINT) {
                            // insert at select waypoint
                            insertAt(((Math.PI / 180) * fw.Latitude), ((Math.PI / 180) * fw.Longitude), 0);
                        }
                        else if (state == MenuState.SELECT_DESTINATION_WAYPOINT) {
                            // destination
                            moveTo(fw);
                        }

                        // next state
                        changeState(MenuState.MOVE);
                    }
                    break;
                case ID.EVENT_MENU_SELECT_MOVE:
                    //
                    // select move
                    //
                    SIMCONNECT_TEXT_RESULT trMove = (SIMCONNECT_TEXT_RESULT)data.dwData;
                    menuDisplayed(trMove, (int)data.uEventID);
                    doMove(trMove);
                    break;
                case ID.EVENT_TITLE_MENU_AI_POSITION:
                    requestFleetPosition();
                    break;
            }
        }

        void sc_OnRecvAssignedObjectId(SimConnect sender, SIMCONNECT_RECV_ASSIGNED_OBJECT_ID data) {
            if (data.dwRequestID >= (int)ID.LAST) {
                int cid = (int)data.dwObjectID;
                int fid = (int)data.dwRequestID - (int)ID.LAST;
                units[fid].setContainerId(cid);
            }
        }

        void sc_OnRecvEventObjectAddremove(SimConnect sender, SIMCONNECT_RECV_EVENT_OBJECT_ADDREMOVE data) {
            if (state != MenuState.MOVE)
                return;		// objects are not present in other menu modes

            uint cid = data.dwData;
            int nObj = 0;
            if (units != null) {
                for (int i = 0; i < units.Length; i++) {
                    Unit un = units[i];
                    if (un == null)
                        continue;

                    if (un.getContainerId() == cid) {
                        // oops we deleted
                        un.setContainerId(-1);
                    }
                    // count object
                    if (un.getContainerId() != -1) nObj++;
                }
            }
            if (nObj == 0) {
                // all deleted
                changeState(MenuState.FORMATION_TYPE);
                currentFormation = null;
                units = null;
            }
        }

        void sc_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data) {
            switch ((ID)data.dwRequestID) {
                case ID.REQUEST_AI_POSITION:
                    if (destWpt != null) {
                        moveTo(data, destWpt);
                    }
                    break;
                case ID.REQUEST_AI_POSITION_FOR_LOCATE:
                    gotFleetPosition(data);
                    break;
            }
        }

        void sc_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data) {
            Disconnect();
        }

        /* **************************************************************
         * 
         * Error tracking
         * 
         * **************************************************************
         */

        private int[] requestIDs;
        private string[] insertedContainers;
        private int insertIDptr;

        private void initErrorTracking() {
            requestIDs = new int[10];
            insertedContainers = new string[10];

            for (int i = 0; i < requestIDs.Length; i++) {
                requestIDs[i] = -1;
            }
            insertIDptr = 0;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void logInsertionPacket(string container) {
            // potential off by one error in beatlesblog simconnect?
            requestIDs[insertIDptr] = sc.LastSentPacketID - 1;
            insertedContainers[insertIDptr] = container;

            insertIDptr++;
            if (insertIDptr == requestIDs.Length) {
                insertIDptr = 0;
            }
        }

        void sc_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data) {
            int reqId = (int)data.dwSendID;
            if (reqId <= 0)
                return;

            bool found = false;
            // find erroring packet
            for (int i = 0; i < requestIDs.Length; i++) {
                if (requestIDs[i] != -1 && requestIDs[i] == reqId) {
                    // print error message
                    try {
                        found = true;
                        string message = string.Format("Error inserting object \"{0}\": {1}", insertedContainers[i], Enum.GetName(typeof(SIMCONNECT_EXCEPTION), data.dwException));
                        sc.Text(SIMCONNECT_TEXT_TYPE.PRINT_RED, 8.0f, ID.REQUEST_ERROR_TEXT, message);
                        log.Warning(message);
                    }
                    catch (SimConnect.SimConnectException ex) {
                        found = false;
                        log.Warning(ex.ToString());
                    }
                    break;
                }
            }

            if (!found) {
                string message = string.Format("OnRecvException: {0}", Enum.GetName(typeof(SIMCONNECT_EXCEPTION), data.dwException));
                log.Warning(message);
                sc.Text(SIMCONNECT_TEXT_TYPE.PRINT_RED, 8.0f, ID.REQUEST_ERROR_TEXT, message);
            }
        }

        /* **************************************************************
         * 
         * Change state
         * 
         * **************************************************************
         */

        private void changeState(MenuState newState) {
            MenuState oldState = state;
            if (oldState == newState)
                return;

            state = newState;

            if (newState == MenuState.MOVE) {
                // activate request position menu
                try {
                    sc.MenuAddSubItem(ID.EVENT_TITLE_MENU, "Request position",
                        ID.EVENT_TITLE_MENU_AI_POSITION, 0);
                }
                catch (SimConnect.SimConnectException ex) {
                    log.Warning(ex.ToString());
                }
            }
            if (oldState == MenuState.MOVE) {
                try {
                    sc.MenuDeleteSubItem(ID.EVENT_TITLE_MENU,
                            ID.EVENT_TITLE_MENU_AI_POSITION);
                }
                catch (SimConnect.SimConnectException ex) {
                    log.Warning(ex.ToString());
                }
            }
        }

        /* **************************************************************
         * 
         * Menu display
         * 
         * **************************************************************
         */

        private void menuDisplayed(SIMCONNECT_TEXT_RESULT tr, int eventId) {
            if (tr == SIMCONNECT_TEXT_RESULT.DISPLAYED) {
                currentlyDisplayed = eventId;
            }
            else {
                currentlyDisplayed = -1;
            }
        }

        private void showOrHideMenu() {
            if (currentlyDisplayed != -1) {
                try {
                    sc.Text(SIMCONNECT_TEXT_TYPE.MENU, 0.0f, (ID)currentlyDisplayed, string.Empty);
                }
                catch (SimConnect.SimConnectException ex) {
                    log.Warning(ex.ToString());
                }
                currentlyDisplayed = -1;
            }
            else {
                showMenu();
            }
        }

        private void showMenu() {
            switch (state) {
                case MenuState.FORMATION_TYPE:
                    formList.showMenu(sc);
                    break;
                case MenuState.POSITION_TYPE:
                    try {
                        var menu = new List<string>(POSITION_TYPES);
                        menu.Insert(0, "Select position type");
                        menu.Insert(0, MENU_TITLE);
                        sc.Text(SIMCONNECT_TEXT_TYPE.MENU, 0.0f, ID.EVENT_MENU_SELECT_POSITION_TYPE, menu.ToArray());
                    }
                    catch (SimConnect.SimConnectException ex) {
                        log.Warning(ex.ToString());
                    }
                    break;
                case MenuState.SELECT_DESTINATION_WAYPOINT:
                case MenuState.SELECT_WAYPOINT:
                    wpList.showMenu(sc);
                    break;
                case MenuState.MOVE:
                    try {
                        var menu = new List<string>(MOVE_TYPES);
                        menu.Insert(0, "Moves (" + currentFormation.getTitle() + ") : ");
                        menu.Insert(0, MENU_TITLE);
                        sc.Text(SIMCONNECT_TEXT_TYPE.MENU, 5.0f, ID.EVENT_MENU_SELECT_MOVE, menu.ToArray());
                    }
                    catch (SimConnect.SimConnectException ex) {
                        log.Warning(ex.ToString());
                    }
                    break;
            }
        }

        /* **************************************************************
         * 
         * Ships movement
         * 
         * **************************************************************
         */

        private void doMove(SIMCONNECT_TEXT_RESULT tr) {
            if (units == null)
                return;

            switch (tr) {
                case SIMCONNECT_TEXT_RESULT.MENU_SELECT_1:
                    // forward
                    int val;
                    if (!int.TryParse(conf.GetKeyValue("base", "throttle"), out val)) {
                        val = 60;
                    }
                    setThrottle(val);
                    break;
                case SIMCONNECT_TEXT_RESULT.MENU_SELECT_2:
                    // full stop 
                    setThrottle(0);
                    break;
                case SIMCONNECT_TEXT_RESULT.MENU_SELECT_3:
                    // left 10
                    moveTurn(-10);
                    break;
                case SIMCONNECT_TEXT_RESULT.MENU_SELECT_4:
                    // right 10
                    moveTurn(10);
                    break;
                case SIMCONNECT_TEXT_RESULT.MENU_SELECT_5:
                    // left 90
                    moveTurn(-90);
                    break;
                case SIMCONNECT_TEXT_RESULT.MENU_SELECT_6:
                    // right 90
                    moveTurn(90);
                    break;
                case SIMCONNECT_TEXT_RESULT.MENU_SELECT_7:
                    //move to waypoint
                    state = MenuState.SELECT_DESTINATION_WAYPOINT;
                    try {
                        sc.RequestFacilitiesList(SIMCONNECT_FACILITY_LIST_TYPE.WAYPOINT, ID.REQUEST_WAYPOINT_LIST);
                    }
                    catch (SimConnect.SimConnectException ex) {
                        log.Warning(ex.ToString());
                    }
                    // do not call showMenu() now because the facility
                    // list may not be fully filled
                    //showMenu();
                    break;
                case SIMCONNECT_TEXT_RESULT.MENU_SELECT_8:
                    //delete
                    deleteAll();
                    break;
            }
        }

        private void deleteAll() {
            for (int i = 0; i < units.Length; i++) {
                Unit un = units[i];
                if (un == null)
                    continue;

                int cid = un.getContainerId();
                if (cid == -1)
                    continue;

                // send delete command to FS
                try {
                    sc.AIRemoveObject((uint)cid, ID.REQUEST_DELETE_AI);
                }
                catch (SimConnect.SimConnectException ex) {
                    log.Warning(ex.ToString());
                }
            }
            // set to initial state, don't wait for deletion ack
            // this is because can have all stale handles (failed insertions)
            changeState(MenuState.FORMATION_TYPE);
        }

        private void moveTurn(double angleDeg) {
            currentHeading += ((Math.PI / 180) * angleDeg);
            if (currentHeading > (2 * Math.PI)) currentHeading -= (2 * Math.PI);
            if (currentHeading < 0) currentHeading += (2 * Math.PI);

            setHeading();
        }

        private void setHeading() {
            for (int i = 0; i < units.Length; i++) {
                Unit un = units[i];
                if (un == null)
                    continue;

                int cid = un.getContainerId();
                if (cid == -1)
                    continue;

                // send heading value
                try {
                    DesiredHeading dh = new DesiredHeading() { desiredHeading = currentHeading };
                    sc.SetDataOnSimObject((uint)cid, dh);
                }
                catch (SimConnect.SimConnectException ex) {
                    log.Warning(ex.ToString());
                }
            }
        }

        private void setThrottle(double percent) {
            for (int i = 0; i < units.Length; i++) {
                Unit un = units[i];
                if (un == null)
                    continue;

                int cid = un.getContainerId();
                if (cid == -1)
                    continue;

                // send throttle command
                int val = (int)(16383.0 * percent / 100.0);
                try {
                    sc.TransmitClientEvent((uint)cid, ID.EVENT_AI_THROTTLE, (uint)val, SIMCONNECT_GROUP_PRIORITY.HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                }
                catch (SimConnect.SimConnectException ex) {
                    log.Warning(ex.ToString());
                }
            }
        }

        private void moveTo(SIMCONNECT_DATA_FACILITY_WAYPOINT fwp) {
            // memorize destination waypoint
            destWpt = fwp;

            // request position of first ship
            for (int i = 0; i < units.Length; i++) {
                Unit un = units[i];
                if (un == null)
                    continue;

                int cid = un.getContainerId();
                if (cid == -1)
                    continue;

                try {
                    sc.RequestDataOnSimObject(ID.REQUEST_AI_POSITION, (uint)cid, SIMCONNECT_PERIOD.ONCE, typeof(DATA_DEF_POSITION));
                }
                catch (SimConnect.SimConnectException ex) {
                    log.Warning(ex.ToString());
                }
                // only ask for the first ship (they all sail with
                // the same bearing)
                break;
            }
        }

        private void moveTo(SIMCONNECT_RECV_SIMOBJECT_DATA e, SIMCONNECT_DATA_FACILITY_WAYPOINT fwp) {
            // calculate position based on static offset and current position
            var pos = (DATA_DEF_POSITION)e.dwData;
            double curLat = pos.Latitude;
            double curLon = pos.Longitude;

            currentHeading = GeoCalc.headingRadians(curLat, curLon,
                    ((Math.PI / 180) * fwp.Latitude),
                    ((Math.PI / 180) * fwp.Longitude));
            setHeading();

            // clean up
            destWpt = null;
        }

        /* **************************************************************
         * 
         * Insertion
         * 
         * **************************************************************
         */

        private void processPositionMenuEntry(SIMCONNECT_TEXT_RESULT tr) {
            if (tr == SIMCONNECT_TEXT_RESULT.MENU_SELECT_1) {
                // user position
                insertAt(userTracker.getUserLat(), userTracker.getUserLon(), userTracker.getUserHdg());
                changeState(MenuState.MOVE);
            }
            else if (tr == SIMCONNECT_TEXT_RESULT.MENU_SELECT_2) {
                // 1nm ahead
                insertAhead(1);
                changeState(MenuState.MOVE);
            }
            else if (tr == SIMCONNECT_TEXT_RESULT.MENU_SELECT_3) {
                // 5nm ahead
                insertAhead(5);
                changeState(MenuState.MOVE);
            }
            else if (tr == SIMCONNECT_TEXT_RESULT.MENU_SELECT_4) {
                // 10nm ahead
                insertAhead(10);
                changeState(MenuState.MOVE);
            }
            else if (tr == SIMCONNECT_TEXT_RESULT.MENU_SELECT_5) {
                changeState(MenuState.SELECT_WAYPOINT);
                try {
                    sc.RequestFacilitiesList(SIMCONNECT_FACILITY_LIST_TYPE.WAYPOINT, ID.REQUEST_WAYPOINT_LIST);
                }
                catch (SimConnect.SimConnectException ex) {
                    log.Warning(ex.ToString());
                }
                // do not call showMenu() now because the facility
                // list may not be fully filled
            }
            else if (tr == SIMCONNECT_TEXT_RESULT.MENU_SELECT_6) {
                // back
                changeState(MenuState.FORMATION_TYPE);
                showMenu();
            }
        }

        private void insertAhead(double distanceNm) {
            double dist = ConvertUnits.nauticMilesToMeters(distanceNm);
            double userLat = userTracker.getUserLat();
            double userLon = userTracker.getUserLon();
            double userHdg = userTracker.getUserHdg();
            Point2D pos = new Point2D(dist, 0);
            pos.scale(1 / GeoCalc.RADIUS_EARTH_M);
            pos.rotate(userHdg);
            pos.translate(userLat, userLon);
            insertAt(pos.x, pos.y, userHdg);
        }

        /**
         * Insert current formation 
         * @param latitude radians
         * @param longitude radians
         * @param heading heading, in radians
         */
        private void insertAt(double latitude, double longitude, double heading) {
            if (currentFormation == null)
                return;

            currentHeading = heading;
            Point2D offset = new Point2D();

            // basic initposition data
            SIMCONNECT_DATA_INITPOSITION ip = new SIMCONNECT_DATA_INITPOSITION();
            ip.Altitude = 0;
            ip.Airspeed = 0;
            ip.Pitch = 0;
            ip.Bank = 0;
            ip.Heading = ((180 / Math.PI) * heading);
            ip.OnGround = true;

            // allocate an array containing container IDs
            int sz = currentFormation.size();
            units = new Unit[sz];
            for (int i = 0; i < sz; i++) {
                UnitPosition up = currentFormation.getUnitPosition(i);
                units[i] = new Unit(up);
            }

            double scale = (2 * Math.PI / (GeoCalc.RADIUS_EARTH_M * 2 * Math.PI));
            for (int i = 0; i < sz; i++) {
                UnitPosition up = currentFormation.getUnitPosition(i);

                // convert coordinates from relative meters to absolute radians
                offset.set(up.getYOffset(), up.getXOffset());
                offset.rotate(heading);
                offset.scale(scale, scale / Math.Cos(latitude));
                offset.translate(latitude, longitude);

                // simconnect insert
                ip.Latitude = ((180 / Math.PI) * offset.x);
                ip.Longitude = ((180 / Math.PI) * offset.y);

                try {
                    sc.AICreateSimulatedObject(up.getContainerTitle(), ip, (ID)(ID.LAST + i));
                    // log packet for error tracking
                    logInsertionPacket(up.getContainerTitle());
                }
                catch (SimConnect.SimConnectException ex) {
                    log.Warning(ex.ToString());
                }
            }
        }

        /* **************************************************************
         * 
         * Request Fleet position
         * 
         * **************************************************************
         */

        private void requestFleetPosition() {
            // send a request

            // request position of first ship
            foreach (Unit un in units) {
                int cid = un.getContainerId();
                if (cid == -1) continue;

                try {
                    sc.RequestDataOnSimObject(ID.REQUEST_AI_POSITION_FOR_LOCATE, (uint)cid, SIMCONNECT_PERIOD.ONCE, typeof(Position));
                }
                catch (SimConnect.SimConnectException ex) {
                    log.Warning(ex.ToString());
                }
                // only ask for the first ship (they all sail with
                // the same bearing)
                break;
            }
        }

        private void gotFleetPosition(SIMCONNECT_RECV_SIMOBJECT_DATA re) {
            var pos = (Position)re.dwData;
            double fleetLat = pos.Latitude;
            double fleetLon = pos.Longitude;
            //		double fleetHdg = re.getDataFloat64();

            double userLat = userTracker.getUserLat();
            double userLon = userTracker.getUserLon();
            double dist = GeoCalc.distanceRadians(userLat, userLon, fleetLat, fleetLon);
            double hdg = GeoCalc.headingRadians(userLat, userLon, fleetLat, fleetLon) - userTracker.getMagVar();

            // fix heading
            if (hdg < 0)
                hdg += (2 * Math.PI);
            if (hdg > (2 * Math.PI))
                hdg -= (2 * Math.PI);

            //FormatType ft = conf.getEnum("base", "format", FormatType.DMS);
            //Format latFormat = FormatLatLon.getLatitudeFormatter(ft);
            //Format lonFormat = FormatLatLon.getLongitudeFormatter(ft);

            try {
                sc.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, 15.0f, ID.EVENT_TEXT_FLEET_POSITION,
                    string.Format("Fleet is at {0:0.00} NM, course {1:0.0} degrees magnetic, position {2:0.#####}, {3:0.#####}",
                    ConvertUnits.metersToNauticMiles(dist), (180 / Math.PI) * hdg, (180 / Math.PI) * fleetLat, (180 / Math.PI) * fleetLon));
            }
            catch (SimConnect.SimConnectException ex) {
                log.Warning(ex.ToString());
            }
        }
    }
}