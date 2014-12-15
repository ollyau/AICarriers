namespace AICarriers {
    class Unit {
        private readonly UnitPosition position;
        private int containerId = -1;

        public Unit(UnitPosition up) {
            this.position = up;
        }

        public int getContainerId() {
            return containerId;
        }

        public void setContainerId(int containerId) {
            this.containerId = containerId;
        }

        public UnitPosition getPosition() {
            return position;
        }
    }
}
