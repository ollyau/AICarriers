using System;
using System.IO;
using System.Windows.Forms;

namespace AICarriers {
    class NotifyIconContext : ApplicationContext {
        private System.ComponentModel.Container components;
        private NotifyIcon notifyIcon;
        private AICarriersManager aicm;

        static public string ExecutablePath {
            get { return new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath; }
        }

        public NotifyIconContext() {
            new ArgumentParser().Check("log", () => { Log.Instance.ShouldSave = true; });

            components = new System.ComponentModel.Container();
            notifyIcon = new NotifyIcon(components);
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(ExecutablePath);
            notifyIcon.Text = "AI Carriers";
            notifyIcon.ContextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem("Exit", (s, e) => {
                    Application.Exit(); 
                })
            });
            notifyIcon.Visible = true;

            try {
                aicm = new AICarriersManager(Path.GetDirectoryName(ExecutablePath));
            }
            catch (Exception ex) {
                Log.Instance.Error(ex.ToString());
                Application.Exit();
            }
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
