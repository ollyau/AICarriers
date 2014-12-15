using BeatlesBlog.SimConnect;

namespace AICarriers {
    [DataStruct()]
    public struct Position {
        [DataItem("PLANE LATITUDE", "radians")]
        public double Latitude;

        [DataItem("PLANE LONGITUDE", "radians")]
        public double Longitude;

        [DataItem("PLANE HEADING DEGREES TRUE", "radians")]
        public double Heading;

        [DataItem("MAGVAR", "radians")]
        public double MagneticVariation;
    }

    class UserTracker {
        Position UserPosition;

        public UserTracker(SimConnect sc) {
            sc.OnRecvSimobjectData += sc_OnRecvSimobjectData;
            sc.OnRecvOpen += sc_OnRecvOpen;
        }

        void sc_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data) {
            // user position
            try {
                sender.RequestDataOnUserSimObject(ID.REQUEST_USER_POSITION, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, typeof(Position));
            }
            catch (SimConnect.SimConnectException ex) {
                Log.Instance.Warning(ex.ToString());
            }
        }

        void sc_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data) {
            if ((ID)data.dwRequestID == ID.REQUEST_USER_POSITION) {
                UserPosition = (Position)data.dwData;
            }
        }

        public double getUserHdg() {
            return UserPosition.Heading;
        }

        public double getUserLat() {
            return UserPosition.Latitude;
        }

        public double getUserLon() {
            return UserPosition.Longitude;
        }

        public double getMagVar() {
            return UserPosition.MagneticVariation;
        }
    }
}
