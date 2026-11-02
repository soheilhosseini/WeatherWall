using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WeatherWall
{
    // The app's own window: everything the tray menu offers, plus a preview of the wallpaper.
    // Opened by double-clicking the tray icon or "Open WeatherWall"; closing it leaves the app running.
    public class MainForm : Form
    {
        readonly TrayApp app;
        readonly PictureBox preview = new PictureBox();
        readonly Label status = new Label();
        readonly Label location = new Label();
        readonly Label locationMode = new Label();
        readonly ComboBox scene = new ComboBox();
        readonly Label pack = new Label();
        readonly CheckBox autostart = new CheckBox();
        readonly System.Collections.Generic.List<string> sceneKeys;
        string previewPath;
        bool updating;

        public MainForm(TrayApp app)
        {
            this.app = app;
            Text = "WeatherWall";
            Icon = TrayApp.AppIcon;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            ClientSize = new Size(640, 456);

            preview.SetBounds(16, 16, 300, 169);
            preview.SizeMode = PictureBoxSizeMode.Zoom;
            preview.BackColor = Color.FromArgb(32, 32, 32);
            status.SetBounds(16, 194, 300, 44);
            status.Font = new Font(Font, FontStyle.Bold);
            var tagline = new Label
            {
                Text = "Two cats who never let go, on your desktop, in your weather and your time of day.",
                ForeColor = SystemColors.GrayText,
            };
            tagline.SetBounds(16, 242, 300, 44);

            const int x = 336, w = 288;
            location.SetBounds(x, 38, w, 20);
            location.AutoEllipsis = true;
            locationMode.SetBounds(x, 58, w, 20);
            locationMode.ForeColor = SystemColors.GrayText;
            var changeLocation = Button("Change…", x, 82, 120);
            changeLocation.Click += delegate { app.ChooseCity(); };

            scene.DropDownStyle = ComboBoxStyle.DropDownList;
            scene.SetBounds(x, 146, w, 28);
            var choices = TrayApp.SceneChoices();
            sceneKeys = choices.Select(c => c.Key).ToList();
            foreach (var c in choices) scene.Items.Add(c.Value);
            scene.SelectedIndexChanged += delegate
            {
                if (!updating && scene.SelectedIndex >= 0) app.SetScene(sceneKeys[scene.SelectedIndex]);
            };

            pack.SetBounds(x, 210, w, 20);
            pack.AutoEllipsis = true;
            var choosePack = Button("Choose folder…", x, 232, 140);
            choosePack.Click += delegate { app.ChoosePack(); };
            var openPack = Button("Open folder", x + 146, 232, 142);
            openPack.Click += delegate { app.OpenPack(); };

            autostart.Text = "Start with Windows";
            autostart.SetBounds(x, 276, w, 24);
            autostart.CheckedChanged += delegate { if (!updating) app.SetAutostart(autostart.Checked); };

            var version = new Label { Text = "Version " + Updater.CurrentVersionText + " · updates itself", AutoSize = true, Location = new Point(x, 334) };
            var checkUpdates = Button("Check for updates", x, 356, 160);
            checkUpdates.Click += delegate { app.CheckUpdatesNow(); };

            var line = new Label { BorderStyle = BorderStyle.Fixed3D };
            line.SetBounds(16, 400, 608, 2);
            var share = Button("Share…", 16, 412, 100);
            share.Click += delegate { ShareForm.ShowFor(app.CurrentWallpaper); };
            var about = Button("About…", 122, 412, 100);
            about.Click += delegate { AboutForm.ShowOnce(); };
            var refresh = Button("Refresh now", 228, 412, 110);
            refresh.Click += delegate { app.RefreshNow(); };
            var close = Button("Close", 534, 412, 90);
            close.Click += delegate { Close(); };
            CancelButton = close;

            Controls.AddRange(new Control[]
            {
                preview, status, tagline,
                Header("Location", x, 16), location, locationMode, changeLocation,
                Header("Picture", x, 124), scene,
                Header("Images", x, 188), pack, choosePack, openPack,
                autostart,
                Header("Updates", x, 312), version, checkUpdates,
                line, share, about, refresh, close,
            });
            RefreshView();
        }

        Label Header(string text, int x, int y)
        {
            return new Label { Text = text, Font = new Font(Font, FontStyle.Bold), AutoSize = true, Location = new Point(x, y) };
        }

        static Button Button(string text, int x, int y, int width)
        {
            var b = new Button { Text = text };
            b.SetBounds(x, y, width, 30);
            return b;
        }

        // Called by the tray app whenever its state changes.
        public void RefreshView()
        {
            updating = true;
            var s = app.Settings;
            status.Text = app.Status;
            location.Text = s.City;
            locationMode.Text = s.CityIsManual ? "Chosen by you"
                : s.FromWindowsLocation ? "Automatic: where this PC is"
                : "Automatic: from the Windows time zone";
            scene.SelectedIndex = Math.Max(0, sceneKeys.IndexOf(s.SceneOverride));
            pack.Text = s.PackFolder;
            autostart.Checked = Autostart.IsOn;
            updating = false;
            ShowPreview(app.CurrentWallpaper);
        }

        // A small copy, so the 2K file is neither locked nor kept in memory.
        void ShowPreview(string path)
        {
            if (path == previewPath) return;
            previewPath = path;
            var old = preview.Image;
            preview.Image = null;
            if (old != null) old.Dispose();
            if (path == null || !File.Exists(path)) return;
            try
            {
                using (var stream = File.OpenRead(path))
                using (var full = Image.FromStream(stream))
                    preview.Image = new Bitmap(full, new Size(600, 600 * full.Height / full.Width));
            }
            catch (Exception ex)
            {
                Log.Write("preview failed: " + ex.Message);
            }
        }
    }
}
