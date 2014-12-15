using System;

namespace AICarriers {
    class GeoCalc {
        // in METERS
        public static readonly double RADIUS_EARTH_M = 6378137;//	6367176.0

        /**
         * Distance in METERS between points with lat/lon in DEGREES
         * @param lat1
         * @param lon1
         * @param lat2
         * @param lon2
         * @return
         */
        public static double distance(double lat1, double lon1, double lat2, double lon2) {
            lat1 = ((Math.PI / 180) * lat1);
            lat2 = ((Math.PI / 180) * lat2);
            lon1 = ((Math.PI / 180) * lon1);
            lon2 = ((Math.PI / 180) * lon2);

            return RADIUS_EARTH_M * Math.Acos(Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2) + Math.Sin(lat1) * Math.Sin(lat2));
        }

        /**
         * Distance in METERS between points with lat/lon in RADIANS
         * @param lat1
         * @param lon1
         * @param lat2
         * @param lon2
         * @return
         */
        public static double distanceRadians(double lat1, double lon1, double lat2, double lon2) {
            return RADIUS_EARTH_M * Math.Acos(Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2) + Math.Sin(lat1) * Math.Sin(lat2));
        }


        /**
         * returns distance in METERS between points
         * @param p1
         * @param p2
         * @return
         */
        public static double distance(Point2D p1, Point2D p2) {

            double lat1 = p1.x;
            double lon1 = p1.y;
            double lat2 = p2.x;
            double lon2 = p2.y;

            return RADIUS_EARTH_M * Math.Acos(Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2) + Math.Sin(lat1) * Math.Sin(lat2));
        }


        /**
         * Distance in METERS between points with lat/lon in DEGREES and alt offset in METERS
         * @param lat1
         * @param lon1
         * @param lat2
         * @param lon2
         * @return
         */
        public static double distanceWithAltitudeOffset(double lat1, double lon1, double lat2, double lon2, double minAlt) {
            lat1 = ((Math.PI / 180) * lat1);
            lat2 = ((Math.PI / 180) * lat2);
            lon1 = ((Math.PI / 180) * lon1);
            lon2 = ((Math.PI / 180) * lon2);

            return (RADIUS_EARTH_M + minAlt) * Math.Acos(Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2) + Math.Sin(lat1) * Math.Sin(lat2));
        }

        /**
         * Distance in METERS between points with lat/lon in DEGREES and alt offset in METERS
         * @param lat1
         * @param lon1
         * @param lat2
         * @param lon2
         * @return
         */
        public static double distanceWithAltitudeOffset(Point2D p1, Point2D p2, double minAlt) {
            double lat1 = p1.x;
            double lon1 = p1.y;
            double lat2 = p2.x;
            double lon2 = p2.y;

            return (RADIUS_EARTH_M + minAlt) * Math.Acos(Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2) + Math.Sin(lat1) * Math.Sin(lat2));
        }

        /**
         * Heading in RADIANS between two points in RADIANS
         * @param p1
         * @param p2
         * @return
         */
        public static double heading(Point2D p1, Point2D p2) {
            double h;

            double lat1 = p1.x;
            double lon1 = p1.y;
            double lat2 = p2.x;
            double lon2 = p2.y;


            if (Math.Cos(lat1) < 0.0001) {
                // prevent overflow
                if (lat1 > 0) h = 180;
                else h = 0;

                return h;
            }

            double b = Math.Atan2(Math.Sin(lon2 - lon1) * Math.Cos(lat2), Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(lon2 - lon1));
            if (b < 0) b = Math.PI * 2 + b;

            return b;
        }

        /**
         * Heading in RADIANS between two points in DEGREES (sorry)
         * @param p1
         * @param p2
         * @return
         */
        public static double heading(double lat1, double lon1, double lat2, double lon2) {
            double h;

            lat1 = ((Math.PI / 180) * lat1);
            lat2 = ((Math.PI / 180) * lat2);
            lon1 = ((Math.PI / 180) * lon1);
            lon2 = ((Math.PI / 180) * lon2);

            if (Math.Cos(lat1) < 0.0001) {
                // prevent overflow
                if (lat1 > 0) h = 180;
                else h = 0;

                return h;
            }

            double b = Math.Atan2(Math.Sin(lon2 - lon1) * Math.Cos(lat2), Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(lon2 - lon1));
            if (b < 0) b = Math.PI * 2 + b;

            return b;
        }

        /**
         * Heading in RADIANS between two points in RADIANS (sorry)
         * @param p1
         * @param p2
         * @return
         */
        public static double headingRadians(double lat1, double lon1, double lat2, double lon2) {
            double h;
            if (Math.Cos(lat1) < 0.0001) {
                // prevent overflow
                if (lat1 > 0) h = 180;
                else h = 0;

                return h;
            }

            double b = Math.Atan2(Math.Sin(lon2 - lon1) * Math.Cos(lat2), Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(lon2 - lon1));
            if (b < 0) b = Math.PI * 2 + b;

            return b;
        }


        /**
         * Calculate a (LAT,LON) point in RADIANS given a course
         * from an origin point, a distance (in METERS)
         * and an heading (in RADIANS)
         * @param lat origin latitude (RAD)
         * @param lon origin long (RAD)
         * @param distMeters distance (METERS)
         * @param radial radial (RAD)
         * @param result result point to be stored, can be null
         * @return result
         */
        public static Point2D courseFrom(double lat, double lon, double distMeters, double radial, Point2D result) {
            if (result == null)
                result = new Point2D();


            radial = radial % (2 * Math.PI);

            double len = distMeters / GeoCalc.RADIUS_EARTH_M;

            double nlat = Math.Asin(Math.Sin(lat) * Math.Cos(len) + Math.Cos(lat) * Math.Sin(len) * Math.Cos(radial));
            double nlon = lon - Math.Asin(Math.Sin(radial) * Math.Sin(len) / Math.Cos(lat)) + Math.PI;
            nlon = nlon % (2 * Math.PI);
            nlon -= Math.PI;

            result.set(nlat, nlon);
            return result;
        }

        /**
         * Calculate a (LAT,LON) point in RADIANS given a course
         * from an origin point, a distance (in METERS)
         * and an heading (in RADIANS)
         * @param origin origin point (RADIANS)
         * @param distMeters distance (METERS)
         * @param radial radial (RAD)
         * @param result result point to be stored, can be null
         * @return result
         */
        public static Point2D courseFrom(Point2D origin, double dist, double radial, Point2D result) {
            return courseFrom(origin.x, origin.y, dist, radial, result);
        }

        /**
         * Calculate a (LAT,LON) point in RADIANS given a course
         * from an origin point, a distance (in METERS)
         * and an heading (in RADIANS)
         * @param origin origin point (RADIANS)
         * @param distMeters distance (METERS)
         * @param radial radial (RAD)
         * @param result result point to be stored, can be null
         * @return result
         */
        public static Point2D courseFrom(Point2D origin, double dist, double radial) {
            return courseFrom(origin.x, origin.y, dist, radial, null);
        }

    }
}
