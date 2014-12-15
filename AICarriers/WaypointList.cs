using BeatlesBlog.SimConnect;
using System;
using System.Collections.Generic;

namespace AICarriers {
    class WaypointList : ListMenu<SIMCONNECT_DATA_FACILITY_WAYPOINT> {
        private List<SIMCONNECT_DATA_FACILITY_WAYPOINT> wpList = new List<SIMCONNECT_DATA_FACILITY_WAYPOINT>();
        private WaypointComparator comparator;// = new WaypointComparator();

        //private NumberFormat df;
        private UserTracker userTracker;

        public WaypointList(UserTracker ut, SimConnect sc)
            : base(ID.EVENT_MENU_SELECT_WAYPOINT, "Select waypoint") {

            this.userTracker = ut;

            comparator = new WaypointComparator(this);

            sc.OnRecvWaypointList += sc_OnRecvWaypointList;
        }

        void sc_OnRecvWaypointList(SimConnect sender, SIMCONNECT_RECV_WAYPOINT_LIST data) {
            if (data.dwentrynumber == 0) {
                wpList.Clear();
                // reset start index
                setStartIndex(0);
            }

            foreach (SIMCONNECT_DATA_FACILITY_WAYPOINT wp in data.rgData) {
                wpList.Add(wp);
            }

            // last packet
            if ((data.dwentrynumber + 1) >= data.dwoutof) {
                // fix empty list by adding a dumb waypoint
                // at current user position
                if (wpList.Count == 0) {
                    SIMCONNECT_DATA_FACILITY_WAYPOINT wp = new SIMCONNECT_DATA_FACILITY_WAYPOINT();
                    wp.Icao = "No waypoints available; display FS map to load them";
                    wp.Latitude = (180 / Math.PI) * userTracker.getUserLat();
                    wp.Longitude = (180 / Math.PI) * userTracker.getUserLon();
                    wpList.Add(wp);
                }

                // sort by distance
                wpList.Sort(comparator);
                showMenu(sender);
            }
        }

        public void handleAirportList(SimConnect sender, SIMCONNECT_RECV_AIRPORT_LIST list) {
        }

        public void handleNDBList(SimConnect sender, SIMCONNECT_RECV_NDB_LIST list) {
        }

        public void handleVORList(SimConnect sender, SIMCONNECT_RECV_VOR_LIST list) {
        }

        class WaypointComparator : IComparer<SIMCONNECT_DATA_FACILITY_WAYPOINT> {
            WaypointList wl;
            public WaypointComparator(WaypointList wl) {
                this.wl = wl;
            }
            public int Compare(SIMCONNECT_DATA_FACILITY_WAYPOINT o1, SIMCONNECT_DATA_FACILITY_WAYPOINT o2) {
                double d1 = wl.distance(o1);
                double d2 = wl.distance(o2);
                return (int)Math.Sign(d1 - d2);
            }
        }

        private double distance(SIMCONNECT_DATA_FACILITY_WAYPOINT wp) {
            return GeoCalc.distanceRadians(userTracker.getUserLat(), userTracker.getUserLon(), (Math.PI / 180) * wp.Latitude, (Math.PI / 180) * wp.Longitude);
        }

        private double heading(SIMCONNECT_DATA_FACILITY_WAYPOINT wp) {
            return GeoCalc.headingRadians(userTracker.getUserLat(), userTracker.getUserLon(), (Math.PI / 180) * wp.Latitude, (Math.PI / 180) * wp.Longitude);
        }


        protected override SIMCONNECT_DATA_FACILITY_WAYPOINT get(int index) {
            return wpList[index];
        }


        protected override int size() {
            return wpList.Count;
        }


        protected override string ToString(SIMCONNECT_DATA_FACILITY_WAYPOINT wp) {
            double d = (distance(wp) / 1000) / 1.852;
            double head = heading(wp);
            return string.Format("{0} ({1:0.0} NM, {2:0} degrees)", wp.Icao, d, (180 / Math.PI) * head);
        }

    }
}
