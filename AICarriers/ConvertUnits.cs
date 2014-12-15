using System;
using System.Text;

namespace AICarriers {
    class ConvertUnits {
        /**
         * Convert FS latitude to DEGREES latitude
         * @param fsLat
         * @return
         */
        public static double fsLat2Lat(int fsLat) {
            return ((double)fsLat) / 10001750.0 * 90.0;
        }

        /**
         * Convert FS longitude to DEGREES longitude
         * @param fsLon
         * @return
         */
        public static double fsLon2Lon(int fsLon) {
            return (double)(fsLon) / 4294967296.0 * 360.0;
        }

        /**
         * Convert DEGREES latitudes to fs latitude
         * @param latDegrees
         * @return
         */
        public static int lat2fsLat(double latDegrees) {
            return (int)(latDegrees * 10001750.0 / 90.0);
        }

        /**
         * Convert DEGREES longitude to FS longitude
         * @param lon
         * @return
         */
        public static int lon2fsLon(double lonDegrees) {
            return (int)(lonDegrees * 4294967296.0 / 360.0);
        }

        /**
         * Converts an FS Angle16 to DEGREES 
         * @param angle
         * @return
         */
        public static double fsAngleToAngle(int angle) {
            double val = angle / 65536.0 * 360.0;
            //		val = ((int) val * 10000) / 10000;
            return val;
        }

        /**
         * Convert a DEGREE angle to FS angle
         * @param angle
         * @return
         */
        public static int angleToFsAngle(double angle) {
            return (int)((angle * 0x10000) / 360.0);
        }

        /**
         * Convert a BGL Longitude to DEGREE longitude
         * @param bglLon
         * @return
         */
        public static double bglLon2Lon(int bglLon) {
            //
            // (double) Lon = (DWORD) Lon * (360.0 / (3 * 0x10000000)) – 180.0
            // (double) Lat = 90.0 - (DWORD) Lat * (180.0 / (2 * 0x10000000))
            return bglLon * (360.0 / (3 * 0x10000000)) - 180.0;
        }

        /**
         * Convert a DEGREE longitude to BGL longitude
         * @param lonDegrees
         * @return
         */
        public static int lon2BglLon(double lonDegrees) {
            return (int)((180.0 + lonDegrees) / (360.0 / (3 * 0x10000000)));
        }

        public static double bglLat2Lat(int bglLat) {
            return 90.0 - (bglLat * (180.0 / (2 * 0x10000000)));
        }

        public static int lat2BglLat(double lat) {
            return (int)((-lat + 90.0) / (180.0 / (2 * 0x10000000)));
        }

        public static double metersToFeet(double meters) {
            return meters * 3.2808399;
        }

        public static double feetToMeters(double feets) {
            return feets * 0.3048;
        }

        public static double metersToNauticMiles(double meters) {
            return meters / 1852;
        }

        public static double nauticMilesToMeters(double nm) {
            return nm * 1852;
        }

        /**
         * Calculate FSX terrain directory from a coordinate
         * @param latRadians
         * @param lonRadians
         * @return
         */
        public static String calcFSXTerrainDirectory(double latRadians,
                double lonRadians) {
            double latDegrees = (180 / Math.PI) * (latRadians);
            double lonDegrees = (180 / Math.PI) * (lonRadians);
            String dirNumber1Text;
            String dirNumber2Text;

            int dirNumber1 = ((int)(((180.0 + (lonDegrees)) * 12) / 360.0));
            if (dirNumber1 < 10)
                dirNumber1Text = "0" + dirNumber1.ToString(); //$NON-NLS-1$
            else
                dirNumber1Text = dirNumber1.ToString();

            int dirNumber2 = ((int)(((90.0 - (latDegrees)) * 8) / 180.0));
            if (dirNumber2 < 10)
                dirNumber2Text = "0" + dirNumber2.ToString(); //$NON-NLS-1$
            else
                dirNumber2Text = dirNumber2.ToString();

            return dirNumber1Text + dirNumber2Text;
        }

        public static String calcFSXTerrainDirectory(Point2D p) {
            return calcFSXTerrainDirectory(p.x, p.y);
        }

        /**
         * Calculate FSX terrain file
         * @param latRadians
         * @param lonRadians
         * @return
         */
        public static String calcFSXTerrainFile(double latRadians,
                double lonRadians) {
            double latDegrees = (180 / Math.PI) * (latRadians);
            double lonDegrees = (180 / Math.PI) * (lonRadians);
            String fileNumber1Text;
            String fileNumber2Text;

            int fileNumber1 = ((int)(((180.0 + (lonDegrees)) * 96) / 360.0));
            if (fileNumber1 < 10)
                fileNumber1Text = "0" + fileNumber1.ToString(); //$NON-NLS-1$
            else
                fileNumber1Text = fileNumber1.ToString();

            int fileNumber2 = ((int)(((90.0 - (latDegrees)) * 64) / 180.0));
            if (fileNumber2 < 10)
                fileNumber2Text = "0" + fileNumber2.ToString(); //$NON-NLS-1$
            else
                fileNumber2Text = fileNumber2.ToString();

            return fileNumber1Text + fileNumber2Text;
        }

        public static String calcFSXTerrainFile(Point2D p) {
            return calcFSXTerrainFile(p.x, p.y);
        }

        public static String calcFSXTileQuad(double latRadians,
                double lonRadians,
                int level) {
            double lat = (180 / Math.PI) * (latRadians);
            double lon = (180 / Math.PI) * (lonRadians);

            StringBuilder sb = new StringBuilder("");

            double x, y = 90, dx = 240, dy = 180;

            if (lon > 60) {
                sb.Append("1");
                x = 60;
            }
            else {
                sb.Append("0");
                x = -180;
            }

            for (int i = 1; i < level; i++) {
                dx = dx / 2;
                dy = dy / 2;
                double mx = x + dx;
                double my = y - dy;

                if (lon < mx && lat > my) {
                    sb.Append("0");
                }
                else if (lon < mx && lat <= my) {
                    y = my;
                    sb.Append("2");
                }
                else if (lon >= mx && lat > my) {
                    x = mx;
                    sb.Append("1");
                }
                else {
                    x = mx;
                    y = my;
                    sb.Append("3");
                }
            }
            return sb.ToString();

        }

        public static String calcFSXTileQuad(Point2D p,
                int level) {
            return calcFSXTileQuad(p.x, p.y, level);
        }
    }
}
