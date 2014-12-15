using System;

namespace AICarriers {
    class Point2D : ICloneable {
        public double x;
        public double y;

        /**
         * @param x
         * @param y
         */
        public Point2D(double x, double y) {
            this.x = x;
            this.y = y;
        }

        public Point2D()
            : this(0.0, 0.0) {
        }

        public Point2D(Point2D pixel) {
            this.x = pixel.x;
            this.y = pixel.y;
        }

        public void scale(double s1, double s2) {
            x *= s1;
            y *= s2;
        }

        public void scale(double s) {
            scale(s, s);
        }

        public void translate(double t1, double t2) {
            x += t1;
            y += t2;
        }

        public void translate(Point2D p) {
            translate(p.x, p.y);
        }

        /**
         * Rotate this point around a specified center
         * @param xCenter x position of the center
         * @param yCenter y position of the center
         * @param angleRadians
         */
        public void rotate(double xCenter, double yCenter, double angleRadians) {
            double nx = (x - xCenter) * Math.Cos(angleRadians) - (y - yCenter) * Math.Sin(angleRadians);
            double ny = (x - xCenter) * Math.Sin(angleRadians) + (y - yCenter) * Math.Cos(angleRadians);
            this.x = nx + xCenter;
            this.y = ny + yCenter;
        }

        public void rotate(Point2D pCenter, double angleRadians) {
            rotate(pCenter.x, pCenter.y, angleRadians);
        }

        public void rotate(double angleRadians) {
            rotate(0, 0, angleRadians);
        }

        /**
         * Returns the euclidean norm of the vector (0,0) -> (x, y)
         * @return
         */
        public double dist() {
            return Math.Sqrt(x * x + y * y);
        }

        public double distFrom(double dx, double dy) {
            return Math.Sqrt((x - dx) * (x - dx) + (y - dy) * (y - dy));
        }

        public double distFrom(Point2D op) {
            return distFrom(op.x, op.y);
        }

        /**
         * returns the manhattan norm of the vector (0,0) ->  (x,y)
         * @return
         */
        public double norm1() {
            return Math.Abs(x) + Math.Abs(y);
        }

        /**
         * Returns the euclidean norm of the vector (0,0) -> (x, y)
         * @return
         */
        public double norm2() {
            return dist();
        }

        /**
         * Returns the infinite norm of the vector (0,0) -> (x, y)
         * @return
         */
        public double normInf() {
            return Math.Max(x, y);
        }

        public void set(double x, double y) {
            this.x = x;
            this.y = y;
        }

        public void set(Point2D p2d) {
            set(p2d.x, p2d.y);
        }

        public void setFromDegrees(double latDegrees, double lonDegrees) {
            set(((Math.PI / 180) * latDegrees), ((Math.PI / 180) * lonDegrees));
        }

        public void setFromRadians(double latRadians, double lonRadians) {
            set(latRadians, lonRadians);
        }

        public void fromDegrees(double xDeg, double yDeg) {
            set(((Math.PI / 180) * xDeg), ((Math.PI / 180) * yDeg));
        }

        public void swap() {
            double temp = x;
            x = y;
            y = temp;
        }

        public override bool Equals(Object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            Point2D pp = (Point2D)obj;
            return pp.x == x && pp.y == y;
        }

        public override int GetHashCode() {
            int hash = 17;
            hash = hash * 23 + x.GetHashCode();
            hash = hash * 23 + y.GetHashCode();
            return hash;
        }

        public object Clone() {
            return new Point2D(this);
        }

        public override String ToString() {
            return x + "," + y; //$NON-NLS-1$
        }

        public double getX() {
            return x;
        }

        public void setX(double x) {
            this.x = x;
        }

        public double getY() {
            return y;
        }

        public void setY(double y) {
            this.y = y;
        }

        public Point2D toDegrees() {
            return new Point2D(((180 / Math.PI) * x), ((180 / Math.PI) * y));
        }

        public Point2D toRadians() {
            return new Point2D(((Math.PI / 180) * x), ((Math.PI / 180) * y));
        }
    }
}
