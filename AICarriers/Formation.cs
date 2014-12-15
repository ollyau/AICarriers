using System;
using System.Collections;
using System.Collections.Generic;

namespace AICarriers {
    public class Formation : IEnumerable<UnitPosition> {
        private List<UnitPosition> units = new List<UnitPosition>();
        private String title;

        public Formation(String title) {
            this.title = title;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        public IEnumerator<UnitPosition> GetEnumerator() {
            return units.GetEnumerator();
        }

        public Formation addUnit(UnitPosition up) {
            units.Add(up);
            return this;
        }

        public Formation addUnit(String container, double x, double y) {
            return addUnit(new UnitPosition(container, x, y));
        }

        public String getTitle() {
            return title;
        }

        public int size() {
            return units.Count;
        }

        public UnitPosition getUnitPosition(int i) {
            return units[i];
        }


        public override string ToString() {
            return title + ": " + units.ToString();
        }
    }

    public class UnitPosition {
        private string containerTitle;
        private double xOffset;
        private double yOffset;

        public UnitPosition(string containerTitle, double offset, double offset2) {
            this.containerTitle = containerTitle;
            xOffset = offset;
            yOffset = offset2;
        }

        public String getContainerTitle() {
            return containerTitle;
        }
        public double getXOffset() {
            return xOffset;
        }
        public double getYOffset() {
            return yOffset;
        }

        public override String ToString() {
            return string.Format("{0}, {1}, {2}", containerTitle, xOffset, yOffset);
        }
    }
}
