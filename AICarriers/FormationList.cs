using iniLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AICarriers {
    public class FormationList : ListMenu<Formation> {
        private List<Formation> formations = new List<Formation>();

        public FormationList(String confDirectory, Ini conf)
            : base(ID.EVENT_MENU_SELECT_FORMATION, "Select ship or ships formation:") {

            parseConfig(conf);

            // parse configuration files included in subdirectory
            string confdDir = conf.GetKeyValue("base", "confdir");
            if (!string.IsNullOrEmpty(confdDir)) {
                string confDir = Path.Combine(confDirectory, confdDir);
                if ((!string.IsNullOrEmpty(confDir) && Directory.Exists(confDir))) {
                    foreach (string file in Directory.GetFiles(confDir, "*.cfg")) {
                        Ini sc = new Ini(file);
                        parseConfig(sc);
                    }
                }
            }

            // fix empty list
            if (formations.Count == 0) {
                formations.Add(new Formation("No formations configured"));
                formations.Add(new Formation("Check aicarriers.cfg"));
            }
        }

        private void parseConfig(Ini conf) {
            int idx = 0;
            while (true) {
                if (conf.GetCategoryNames().Contains("formation." + idx)) {
                    addFormation(conf, idx);
                    idx++;
                }
                else {
                    break;
                }
            }
        }

        private void addFormation(Ini conf, int index) {
            string section = "formation." + index;
            string title = conf.GetKeyValue(section, "title");
            if (string.IsNullOrEmpty(title))
                return;
            Formation f = new Formation(title);

            int idx = 0;
            while (true) {
                string unitStr = conf.GetKeyValue(section, "unit." + idx);
                if (!string.IsNullOrEmpty(unitStr)) {
                    string[] parts = unitStr.Split(',');
                    if (parts.Count() == 3) {
                        try {
                            f.addUnit(parts[0], Double.Parse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture), Double.Parse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture));
                        }
                        catch (Exception ex) {
                            Log.Instance.Warning(ex.ToString());
                        }
                    }
                    idx++;
                }
                else {
                    break;
                }
            }

            if (f.size() > 0)
                formations.Add(f);
        }

        public override string ToString() {
            return formations.ToString();
        }

        protected override Formation get(int index) {
            return formations[index];
        }

        protected override int size() {
            return formations.Count;
        }

        protected override string ToString(Formation f) {
            return f.getTitle();
        }
    }
}
