using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace WeatherWall
{
    // Why WeatherWall exists, the version, and where to find the project.
    public class AboutForm : Form
    {
        public const string RepoUrl = "https://github.com/soheilhosseini/WeatherWall";

        // The same story opens the README.
        public const string Story =
            "Someone I love is moving to another country to study, and for a while we will live far apart.\n\n" +
            "I made WeatherWall so that every time she looks at her computer, she sees these two cats on her " +
            "desktop — always holding each other, always side by side. Sun or rain, morning or late at night, " +
            "they are together.\n\n" +
            "We may be far apart in distance, but our hearts are always side by side. That is what these " +
            "pictures say.";

        static AboutForm open;

        public static void ShowOnce()
        {
            if (open == null || open.IsDisposed) open = new AboutForm();
            open.Show();
            open.Activate();
        }

        AboutForm()
        {
            Text = "About WeatherWall";
            Icon = TrayApp.AppIcon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            ClientSize = new Size(480, 430);

            var logo = new PictureBox { Image = new Icon(TrayApp.AppIcon, 48, 48).ToBitmap(), SizeMode = PictureBoxSizeMode.Zoom };
            logo.SetBounds(16, 16, 48, 48);
            var title = new Label { Text = "WeatherWall", Font = new Font("Segoe UI", 15f, FontStyle.Bold), AutoSize = true, Location = new Point(76, 12) };
            var version = new Label
            {
                Text = "Version " + Updater.CurrentVersionText + " · free and open source (MIT)",
                AutoSize = true, Location = new Point(78, 44), ForeColor = SystemColors.GrayText,
            };

            var storyTitle = new Label { Text = "Why it exists", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Location = new Point(16, 84) };
            var story = new Label { Text = Story };
            story.SetBounds(16, 108, 448, 172);

            var repo = Link(RepoUrl.Replace("https://", ""), RepoUrl, 290);
            var issues = Link("Report a problem or suggest an idea", RepoUrl + "/issues", 314);
            var weather = Link("Weather data by Open-Meteo.com", "https://open-meteo.com", 338);

            var close = new Button { Text = "Close", DialogResult = DialogResult.Cancel };
            close.SetBounds(374, 384, 90, 30);
            close.Click += delegate { Close(); };
            CancelButton = close;

            Controls.AddRange(new Control[] { logo, title, version, storyTitle, story, repo, issues, weather, close });
        }

        static LinkLabel Link(string text, string url, int y)
        {
            var link = new LinkLabel { Text = text, AutoSize = true, Location = new Point(16, y) };
            link.LinkClicked += delegate { OpenUrl(url); };
            return link;
        }

        public static void OpenUrl(string url)
        {
            try { Process.Start(url); }
            catch (Exception ex) { Log.Write("could not open " + url + ": " + ex.Message); }
        }
    }
}
