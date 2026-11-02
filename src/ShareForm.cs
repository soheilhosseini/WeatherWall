using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WeatherWall
{
    // Send the project link to someone, or keep the current wallpaper as a picture.
    public class ShareForm : Form
    {
        const string Message =
            "WeatherWall: two cats who never let go, on your Windows wallpaper. " +
            "They follow the real weather, the time of day and the holidays.";

        static ShareForm open;
        readonly string wallpaper;
        readonly Label copied = new Label();

        public static void ShowFor(string wallpaperPath)
        {
            if (open != null && !open.IsDisposed) open.Close();
            open = new ShareForm(wallpaperPath);
            open.Show();
            open.Activate();
        }

        ShareForm(string wallpaperPath)
        {
            wallpaper = wallpaperPath;
            Text = "Share WeatherWall";
            Icon = TrayApp.AppIcon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            ClientSize = new Size(440, 290);

            var title = new Label { Text = "Share WeatherWall", Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(16, 14) };
            var intro = new Label
            {
                Text = "Someone far from the one they love? Send them WeatherWall: two cats who never let go, on their desktop.",
                ForeColor = SystemColors.GrayText,
            };
            intro.SetBounds(16, 44, 408, 40);

            var link = new TextBox { Text = AboutForm.RepoUrl, ReadOnly = true };
            link.SetBounds(16, 92, 312, 26);
            var copy = new Button { Text = "Copy link" };
            copy.SetBounds(334, 91, 90, 28);
            copy.Click += delegate { CopyLink(); };
            copied.SetBounds(16, 122, 408, 20);
            copied.ForeColor = Color.SeaGreen;

            var telegram = new Button { Text = "Telegram" };
            telegram.SetBounds(16, 148, 120, 30);
            telegram.Click += delegate
            {
                // core.telegram.org/widgets/share
                AboutForm.OpenUrl("https://t.me/share/url?url=" + Uri.EscapeDataString(AboutForm.RepoUrl) +
                                  "&text=" + Uri.EscapeDataString(Message));
            };
            var email = new Button { Text = "Email" };
            email.SetBounds(142, 148, 120, 30);
            email.Click += delegate
            {
                AboutForm.OpenUrl("mailto:?subject=" + Uri.EscapeDataString("WeatherWall") +
                                  "&body=" + Uri.EscapeDataString(Message + "\n\n" + AboutForm.RepoUrl));
            };

            var save = new Button { Text = "Save this wallpaper as a picture…" };
            save.SetBounds(16, 196, 260, 30);
            save.Enabled = wallpaper != null && File.Exists(wallpaper);
            save.Click += delegate { SavePicture(); };

            var close = new Button { Text = "Close" };
            close.SetBounds(334, 244, 90, 30);
            close.Click += delegate { Close(); };
            CancelButton = close;

            Controls.AddRange(new Control[] { title, intro, link, copy, copied, telegram, email, save, close });
            ActiveControl = copy;
        }

        void CopyLink()
        {
            try
            {
                Clipboard.SetText(AboutForm.RepoUrl);
                copied.Text = "Link copied — paste it anywhere.";
            }
            catch (Exception ex)
            {
                copied.Text = "Could not copy: " + ex.Message;
            }
        }

        void SavePicture()
        {
            using (var dlg = new SaveFileDialog
            {
                FileName = "WeatherWall " + Path.GetFileName(wallpaper),
                Filter = "Picture|*" + Path.GetExtension(wallpaper),
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try { File.Copy(wallpaper, dlg.FileName, true); }
                catch (Exception ex) { MessageBox.Show(this, "Could not save: " + ex.Message, "WeatherWall"); }
            }
        }
    }
}
