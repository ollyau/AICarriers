using BeatlesBlog.SimConnect;

namespace AICarriers {
    public enum ID {
        DATA_DEF_POSITION,
        DATA_DEF_DESIRED_HEADING,

        REQUEST_USER_POSITION,
        REQUEST_WAYPOINT_LIST,
        REQUEST_AI_POSITION,
        REQUEST_AI_POSITION_FOR_LOCATE,
        REQUEST_ERROR_TEXT,

        REQUEST_CREATE_AI,
        REQUEST_DELETE_AI,

        EVENT_SIMSTART,
        EVENT_OBJECT_REMOVED,
        EVENT_TITLE_MENU,
        EVENT_TITLE_MENU_SHOW,
        EVENT_TITLE_MENU_AI_POSITION,

        EVENT_CALL_MENU,

        EVENT_MENU_SELECT_FORMATION,
        EVENT_MENU_SELECT_WAYPOINT,
        EVENT_MENU_SELECT_POSITION_TYPE,
        EVENT_MENU_SELECT_MOVE,

        EVENT_TEXT_FLEET_POSITION,

        EVENT_AI_THROTTLE,

        GROUP_KEYS,
        INPUT_KEYS,

        LAST
    }

    [DataStruct()]
    public struct DesiredHeading {
        [DataItem("AI DESIRED HEADING", "radians")]
        public double desiredHeading;
    }

    [DataStruct()]
    public struct DATA_DEF_POSITION {
        [DataItem("PLANE LATITUDE", "radians")]
        public double Latitude;

        [DataItem("PLANE LONGITUDE", "radians")]
        public double Longitude;
    }
}
