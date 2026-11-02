using System;
using System.Drawing;
using System.Windows.Forms;

namespace WeatherWall
{
    // Search a city by name and pick one of the matches.
    public class CityForm : Form
    {
        readonly TextBox query = new TextBox();
        readonly Button search = new Button();
        readonly ListBox results = new ListBox();
        readonly Button ok = new Button();
        readonly Label status = new Label();

        public City Selected { get; private set; }

        public CityForm(string current)
        {
            Text = "WeatherWall: choose city";
            Icon = TrayApp.AppIcon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(420, 320);
            Font = new Font("Segoe UI", 9.5f);

            var now = new Label { Text = "Current: " + current, Location = new Point(12, 12), AutoSize = true };
            query.SetBounds(12, 40, 300, 26);
            search.Text = "Search";
            search.SetBounds(320, 39, 88, 28);
            results.SetBounds(12, 76, 396, 190);
            status.SetBounds(12, 280, 250, 24);
            ok.Text = "Use this city";
            ok.SetBounds(270, 276, 138, 30);
            ok.Enabled = false;

            search.Click += delegate { DoSearch(); };
            query.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; DoSearch(); } };
            results.SelectedIndexChanged += delegate { ok.Enabled = results.SelectedItem != null; };
            results.DoubleClick += delegate { Accept(); };
            ok.Click += delegate { Accept(); };

            Controls.AddRange(new Control[] { now, query, search, results, status, ok });
            AcceptButton = search;
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
                status.Text = results.Items.Count == 0 ? "No city found." : "Pick one and press the button.";
                if (results.Items.Count > 0) results.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                status.Text = "Search failed: " + ex.Message;
            }
        }

        void Accept()
        {
            Selected = results.SelectedItem as City;
            if (Selected == null) return;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
