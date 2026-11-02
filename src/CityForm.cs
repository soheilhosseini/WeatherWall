using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace WeatherWall
{
    // Location window: automatic (Windows location, else the Windows time zone) or an exact city.
    public class CityForm : Form
    {
        readonly RadioButton autoChoice = new RadioButton();
        readonly Label autoDetail = new Label();
        readonly LinkLabel locationSettings = new LinkLabel();
        readonly RadioButton manualChoice = new RadioButton();
        readonly TextBox query = new TextBox();
        readonly Button search = new Button();
        readonly ListBox results = new ListBox();
        readonly Label status = new Label();
        readonly Button save = new Button();
        readonly Button cancel = new Button();

        public bool Automatic { get { return autoChoice.Checked; } }
        public City Selected { get; private set; }

        public CityForm(bool manual, string currentCity)
        {
            Text = "WeatherWall — Location";
            Icon = TrayApp.AppIcon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            ClientSize = new Size(460, 430);

            var intro = new Label
            {
                Text = "The weather and the time of day come from this place.",
                Location = new Point(16, 14), AutoSize = true, ForeColor = SystemColors.GrayText,
            };

            autoChoice.Text = "Automatic — where this PC is (recommended)";
            autoChoice.Font = new Font(Font, FontStyle.Bold);
            autoChoice.SetBounds(16, 44, 430, 24);
            autoDetail.SetBounds(36, 70, 410, 40);
            autoDetail.Text = "Asking Windows where this PC is…";
            locationSettings.Text = "Windows location settings…";
            locationSettings.Location = new Point(36, 112);
            locationSettings.AutoSize = true;
            locationSettings.LinkClicked += delegate
            {
                try { Process.Start("ms-settings:privacy-location"); }
                catch (Exception) { }
            };

            manualChoice.Text = "Choose an exact city";
            manualChoice.Font = new Font(Font, FontStyle.Bold);
            manualChoice.SetBounds(16, 144, 430, 24);
            var hint = new Label
            {
                Text = "Type a city name, press Search, then pick it from the list.",
                Location = new Point(36, 170), AutoSize = true, ForeColor = SystemColors.GrayText,
            };
            query.SetBounds(36, 196, 312, 26);
            search.Text = "Search";
            search.SetBounds(356, 195, 90, 28);
            results.SetBounds(36, 230, 410, 128);
            status.SetBounds(36, 362, 410, 20);
            status.ForeColor = SystemColors.GrayText;
            if (manual) status.Text = "Current: " + currentCity;

            save.Text = "Save";
            save.SetBounds(262, 390, 90, 30);
            cancel.Text = "Cancel";
            cancel.SetBounds(356, 390, 90, 30);
            cancel.DialogResult = DialogResult.Cancel;
            CancelButton = cancel;

            autoChoice.CheckedChanged += delegate { UpdateEnabled(); };
            manualChoice.CheckedChanged += delegate { UpdateEnabled(); };
            search.Click += delegate { DoSearch(); };
            query.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; DoSearch(); } };
            query.Enter += delegate { manualChoice.Checked = true; };
            results.SelectedIndexChanged += delegate { UpdateEnabled(); };
            results.DoubleClick += delegate { Save(); };
            save.Click += delegate { Save(); };

            Controls.AddRange(new Control[]
                { intro, autoChoice, autoDetail, locationSettings, manualChoice, hint, query, search, results, status, save, cancel });
            autoChoice.Checked = !manual;
            manualChoice.Checked = manual;
            UpdateEnabled();
            Shown += delegate { ShowAutoCity(); };
        }

        // The same order the tray app uses: the Windows position, else the time-zone city. Runs on a
        // pool thread, since Windows can take a few seconds to answer.
        void ShowAutoCity()
        {
            var zone = AutoCity.CurrentWindowsZone();
            var zoneName = AutoCity.CurrentZoneDisplayName();
            ThreadPool.QueueUserWorkItem(delegate
            {
                City city = null;
                try { city = AutoCity.Resolve(zone); }
                catch (Exception) { }
                double lat, lon;
                var outcome = WindowsLocation.Outcome.NoFix;
                try { outcome = WindowsLocation.TryGet(TimeSpan.FromSeconds(15), out lat, out lon); }
                catch (Exception) { lat = lon = 0; }
                string text;
                if (outcome == WindowsLocation.Outcome.Found)
                    text = "From Windows location:\n→ " + WindowsLocation.LabelFor(lat, lon, city);
                else
                    text = (outcome == WindowsLocation.Outcome.Off
                               ? "Windows location is off, so the time zone is used:\n"
                               : "Windows has no position yet, so the time zone is used:\n")
                           + zoneName + " → " + (city != null ? city.Label : "no city (or offline) — choose one below");
                try { BeginInvoke((Action)delegate { autoDetail.Text = text; }); }
                catch (InvalidOperationException) { }   // the window was closed meanwhile
            });
        }

        void UpdateEnabled()
        {
            var manual = manualChoice.Checked;
            query.Enabled = search.Enabled = results.Enabled = manual;
            save.Enabled = !manual || results.SelectedItem != null;
        }

        void DoSearch()
        {
            var q = query.Text.Trim();
            if (q.Length < 2) return;
            results.Items.Clear();
            status.Text = "Searching…";
            Refresh();
            try
            {
                foreach (var c in WeatherClient.SearchCity(q)) results.Items.Add(c);
                status.Text = results.Items.Count == 0 ? "No city found." : "Pick one, then Save.";
                if (results.Items.Count > 0) results.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                status.Text = "Search failed: " + ex.Message;
            }
            UpdateEnabled();
        }

        void Save()
        {
            if (manualChoice.Checked)
            {
                Selected = results.SelectedItem as City;
                if (Selected == null) return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
