using System;
using System.Drawing;
using System.Windows.Forms;

namespace WeatherWall
{
    // Location window: follow the Windows time zone (default) or pick an exact city.
    public class CityForm : Form
    {
        readonly RadioButton autoChoice = new RadioButton();
        readonly Label autoDetail = new Label();
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

            autoChoice.Text = "Automatic — follow the Windows time zone (recommended)";
            autoChoice.Font = new Font(Font, FontStyle.Bold);
            autoChoice.SetBounds(16, 44, 430, 24);
            autoDetail.SetBounds(36, 70, 410, 40);
            autoDetail.Text = AutoCity.CurrentZoneDisplayName() + "\nLooking up the city…";

            manualChoice.Text = "Choose an exact city";
            manualChoice.Font = new Font(Font, FontStyle.Bold);
            manualChoice.SetBounds(16, 122, 430, 24);
            var hint = new Label
            {
                Text = "Type a city name, press Search, then pick it from the list.",
                Location = new Point(36, 148), AutoSize = true, ForeColor = SystemColors.GrayText,
            };
            query.SetBounds(36, 174, 312, 26);
            search.Text = "Search";
            search.SetBounds(356, 173, 90, 28);
            results.SetBounds(36, 208, 410, 150);
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
                { intro, autoChoice, autoDetail, manualChoice, hint, query, search, results, status, save, cancel });
            autoChoice.Checked = !manual;
            manualChoice.Checked = manual;
            UpdateEnabled();
            Shown += delegate { ShowAutoCity(); };
        }

        void ShowAutoCity()
        {
            Refresh();
            City city = null;
            try { city = AutoCity.Resolve(AutoCity.CurrentWindowsZone()); }
            catch (Exception) { }
            autoDetail.Text = AutoCity.CurrentZoneDisplayName() + "\n→ " +
                (city != null ? city.Label : "no city for this time zone (or offline) — choose one below");
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
