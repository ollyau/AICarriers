using BeatlesBlog.SimConnect;
using System;
using System.Collections.Generic;

namespace AICarriers {
    public abstract class ListMenu<T> {
        protected abstract int size();
        protected abstract T get(int index);
        protected abstract String ToString(T obj);

        private int startIndex = 0;

        private ID eventId;
        private String prompt;

        public ListMenu(ID eventId, String prompt) {
            this.eventId = eventId;
            this.prompt = prompt;

            if (prompt == null)
                prompt = "Select";

            if (eventId == null)
                eventId = ID.LAST;		// oops
        }

        protected void setStartIndex(int index) {
            this.startIndex = index;
        }

        public T processEvent(SimConnect sc, SIMCONNECT_TEXT_RESULT tr) {
            if ((int)tr > 10)
                return default(T);

            int index = (int)tr;

            if (startIndex != 0 && index == 0) {
                // previous page
                startIndex -= 8;
                showMenu(sc);
                return default(T);
            }

            int n = size();
            if (index == 9 && (n > (startIndex + index))) {
                // is next page
                startIndex += 8;
                showMenu(sc);
                return default(T);
            }

            if (startIndex != 0)
                index--;	// first entry is previous

            return get(index + startIndex);
        }

        private List<String> sv = new List<String>(10);

        public void showMenu(SimConnect sc) {
            // clear menu items
            sv.Clear();

            int sz = size();
            int n = 10;
            if (startIndex != 0) n--;	// need "previous page" item
            n = Math.Min(n, sz - startIndex);		// too much items
            if (sz > (n + startIndex)) n--;	// need "Next page" item

            if (0 != startIndex) sv.Add("Previous page");
            for (int i = 0; i < n; i++) {
                T obj = get(startIndex + i);
                sv.Add(ToString(obj));
            }
            if (sz > (n + startIndex)) sv.Add("Next page");

            var menu = new List<string>(sv);
            menu.Insert(0, prompt);
            menu.Insert(0, AICarriersManager.MENU_TITLE);
            try {
                sc.Text(SIMCONNECT_TEXT_TYPE.MENU, 15.0f, eventId, menu.ToArray());
            }
            catch (SimConnect.SimConnectException ex) {
                Log.Instance.Warning(ex.ToString());
            }
        }

    }
}
