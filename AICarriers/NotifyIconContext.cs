using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace AICarriers {
    class NotifyIconContext : ApplicationContext {
        private System.ComponentModel.Container components;
        private NotifyIcon notifyIcon;
        private AICarriersManager aicm;

        static public string ExecutablePath {
            get { return new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath; }
        }

        // from https://stackoverflow.com/a/580264
        public void SetNotifyIconText(NotifyIcon ni, string text) {
            if (text.Length >= 128) throw new ArgumentOutOfRangeException("Text limited to 127 characters");
            Type t = typeof(NotifyIcon);
            BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
            t.GetField("text", hidden).SetValue(ni, text);
            if ((bool)t.GetField("added", hidden).GetValue(ni))
                t.GetMethod("UpdateIcon", hidden).Invoke(ni, new object[] { true });
        }

        public NotifyIconContext() {
            new ArgumentParser().Check("log", () => { Log.Instance.ShouldSave = true; });

            components = new System.ComponentModel.Container();
            notifyIcon = new NotifyIcon(components);
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(ExecutablePath);

            SetNotifyIconText(notifyIcon, string.Format("AI Carriers ({0})\r\nNot connected",
                Assembly.GetExecutingAssembly().GetName().Version));

            notifyIcon.ContextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem("Exit", (s, e) => { Application.Exit(); })
            });
            notifyIcon.Visible = true;

            try {
                aicm = new AICarriersManager(Path.GetDirectoryName(ExecutablePath));
                aicm.OpenEvent += aicm_OpenEvent;
                aicm.DisconnectEvent += aicm_DisconnectEvent;
            }
            catch (Exception ex) {
                Log.Instance.Error(ex.ToString());
                Application.Exit();
            }
        }

        void aicm_OpenEvent(object sender, OpenEventArgs e) {
            SetNotifyIconText(notifyIcon, string.Format("AI Carriers ({0})\r\nConnected to {1}",
                Assembly.GetExecutingAssembly().GetName().Version,
                e.SimulatorName));
        }

        void aicm_DisconnectEvent(object sender, EventArgs e) {
            Application.Exit();
        }

        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                if (aicm != null) { aicm.Disconnect(); }
                Log.Instance.ConditionalSave();
                notifyIcon.Dispose();
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
