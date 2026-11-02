using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WeatherWall
{
    // Lives in the notification area next to the clock. Fetches the weather every hour and
    // re-checks the time of day every five minutes, changing the wallpaper only when needed.
    public class TrayApp : ApplicationContext
    {
        public static readonly Icon AppIcon = LoadIcon();

        readonly Settings settings = Settings.Load();
        readonly NotifyIcon tray = new NotifyIcon();
        readonly ToolStripMenuItem statusItem = new ToolStripMenuItem { Enabled = false };
        readonly ToolStripMenuItem cityItem = new ToolStripMenuItem();
        readonly ToolStripMenuItem sceneMenu = new ToolStripMenuItem("Scene");
        readonly ToolStripMenuItem autostartItem = new ToolStripMenuItem("Start with Windows");
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 5 * 60 * 1000 };
        // First update check 30 s after start, then once a day.
        readonly System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer { Interval = 30 * 1000 };
        readonly ToolStripMenuItem updateItem = new ToolStripMenuItem();
        bool updateRunning;

        SynchronizationContext ui;
        Weather weather;
        string currentPath;
        string lastError;

        public TrayApp(bool justUpdated)
        {
            var firstRun = !settings.FirstRunDone;
            if (firstRun)
            {
                Autostart.Set(true, Application.ExecutablePath);
                settings.FirstRunDone = true;
                settings.Save();
            }

            BuildMenu();
            ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            tray.Icon = AppIcon;
            tray.Visible = true;
            tray.DoubleClick += delegate { ChooseCity(); };

            timer.Tick += delegate { Tick(false); };
            timer.Start();
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            updateTimer.Tick += delegate { updateTimer.Interval = 24 * 60 * 60 * 1000; CheckForUpdates(false); };
            // A fresh install has no images yet: fetch the pack right away instead of in 30 s.
            if (PackIsManagedByUpdates && PackIsEmpty) updateTimer.Interval = 2000;
            updateTimer.Start();
            if (justUpdated)
            {
                tray.ShowBalloonTip(5000, "WeatherWall", "Updated to version " + Updater.CurrentVersionText, ToolTipIcon.Info);
                // The old process may still hold its exe for a moment after handing over; retry later.
                var cleanup = new System.Windows.Forms.Timer { Interval = 15000 };
                cleanup.Tick += delegate { cleanup.Stop(); cleanup.Dispose(); Updater.CleanUpOldExe(); };
                cleanup.Start();
            }
            else if (firstRun)
                tray.ShowBalloonTip(6000, "WeatherWall is running",
                    "It lives here, next to the clock. Right-click to pick your city.", ToolTipIcon.Info);
            Tick(true);
        }

        bool PackIsEmpty
        {
            get { return !Directory.Exists(settings.PackFolder) || !Directory.EnumerateFiles(settings.PackFolder).Any(); }
        }

        bool PackIsManagedByUpdates
        {
            get { return string.Equals(Path.GetFullPath(settings.PackFolder).TrimEnd('\\'),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pack").TrimEnd('\\'), StringComparison.OrdinalIgnoreCase); }
        }

        // A missing or failing pack repository must never block app updates.
        static string TryUpdatePack(string packFolder, string packSignature)
        {
            try
            {
                var packRelease = Updater.LatestRelease(Updater.PackRepo);
                var signature = Updater.UpdatePackIfChanged(packRelease, packFolder, packSignature);
                Log.Write("pack check: latest " + packRelease.Tag + ", " + (signature != null ? "updated" : "current"));
                return signature;
            }
            catch (Exception ex)
            {
                Log.Write("pack check failed: " + ex);
                return null;
            }
        }

        // Network work runs on a pool thread; anything that touches the UI is posted back.
        void CheckForUpdates(bool userAsked)
        {
            if (updateRunning) return;
            updateRunning = true;
            var managedPack = PackIsManagedByUpdates;
            var packFolder = settings.PackFolder;
            var packSignature = settings.PackSignature;
            ThreadPool.QueueUserWorkItem(delegate
            {
                string message = null;
                string newPackSignature = null;
                bool restart = false;
                try
                {
                    if (managedPack) newPackSignature = TryUpdatePack(packFolder, packSignature);
                    var release = Updater.LatestRelease(Updater.Repo);
                    restart = Updater.UpdateAppIfNewer(release);
                    Log.Write("app check: latest " + release.Tag + ", " + (restart ? "updated" : "current"));
                    if (userAsked && !restart && newPackSignature == null) message = "You have the latest version (" + Updater.CurrentVersionText + ").";
                }
                catch (Exception ex)
                {
                    Log.Write("update check failed: " + ex);
                    if (userAsked) message = "Update check failed: " + ex.Message;
                }
                ui.Post(delegate
                {
                    updateRunning = false;
                    if (newPackSignature != null)
                    {
                        settings.PackSignature = newPackSignature;
                        settings.Save();
                        currentPath = null;
                        Apply();
                    }
                    if (restart)
                    {
                        Process.Start(Application.ExecutablePath, "--updated");
                        ExitThread();
                        return;
                    }
                    if (message != null) tray.ShowBalloonTip(4000, "WeatherWall", message, ToolTipIcon.Info);
                }, null);
            });
        }

        static Icon LoadIcon()
        {
            try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch (Exception) { return SystemIcons.Application; }
        }

        void BuildMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add(statusItem);
            menu.Items.Add(new ToolStripSeparator());
            cityItem.Click += delegate { ChooseCity(); };
            menu.Items.Add(cityItem);

            var names = new[] { "auto" }.Concat(Scenes.All).Concat(Occasions.All.Select(o => "occasion:" + o));
            foreach (var name in names)
            {
                var label = name == "auto" ? "Auto (live weather)"
                    : name.StartsWith("occasion:") ? "Occasion: " + Title(name.Substring(9).Replace('_', ' '))
                    : Title(name);
                var item = new ToolStripMenuItem(label) { Tag = name };
                item.Click += (s, e) =>
                {
                    settings.SceneOverride = (string)((ToolStripMenuItem)s).Tag;
                    settings.Save();
                    Apply();
                };
                sceneMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(sceneMenu);

            var refresh = new ToolStripMenuItem("Refresh now");
            refresh.Click += delegate { Tick(true); };
            menu.Items.Add(refresh);

            var pack = new ToolStripMenuItem("Choose image folder…");
            pack.Click += delegate { ChoosePack(); };
            menu.Items.Add(pack);

            var open = new ToolStripMenuItem("Open image folder");
            open.Click += delegate
            {
                if (Directory.Exists(settings.PackFolder)) Process.Start("explorer.exe", "\"" + settings.PackFolder + "\"");
            };
            menu.Items.Add(open);

            autostartItem.Click += delegate
            {
                Autostart.Set(!Autostart.IsOn, Application.ExecutablePath);
                UpdateMenu();
            };
            menu.Items.Add(autostartItem);
            updateItem.Text = "Version " + Updater.CurrentVersionText + " · Check for updates";
            updateItem.Click += delegate { CheckForUpdates(true); };
            menu.Items.Add(updateItem);
            menu.Items.Add(new ToolStripSeparator());

            var exit = new ToolStripMenuItem("Exit");
            exit.Click += delegate { ExitThread(); };
            menu.Items.Add(exit);

            menu.Opening += delegate { UpdateMenu(); };
            tray.ContextMenuStrip = menu;
            UpdateMenu();
        }

        static string Title(string s)
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);
        }

        void UpdateMenu()
        {
            cityItem.Text = "City: " + settings.City + "…";
            autostartItem.Checked = Autostart.IsOn;
            foreach (ToolStripMenuItem item in sceneMenu.DropDownItems)
                item.Checked = (string)item.Tag == settings.SceneOverride;
            statusItem.Text = StatusText();
            var tip = "WeatherWall\n" + StatusText();
            tray.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip;
        }

        string StatusText()
        {
            if (lastError != null) return "⚠ " + lastError;
            if (weather == null) return "Fetching weather…";
            var occasion = CurrentOccasion();
            var scene = occasion != null ? Title(occasion.Replace('_', ' ')) : Title(CurrentScene());
            if (settings.SceneOverride != "auto") scene += " (pinned)";
            return string.Format(CultureInfo.InvariantCulture, "{0:0}°C · {1} · {2}",
                weather.TemperatureC, scene, Title(Scenes.SlotFor(weather, weather.CityNow)));
        }

        string CurrentOccasion()
        {
            var pin = settings.SceneOverride;
            if (pin.StartsWith("occasion:")) return pin.Substring(9);
            return pin == "auto" ? Occasions.For(weather.CityNow) : null;
        }

        string CurrentScene()
        {
            var pin = settings.SceneOverride;
            return pin == "auto" || pin.StartsWith("occasion:") ? Scenes.SceneFor(weather) : pin;
        }

        void Tick(bool forceFetch)
        {
            if (forceFetch || weather == null || DateTime.UtcNow - weather.FetchedUtc >= TimeSpan.FromMinutes(60))
            {
                try
                {
                    weather = WeatherClient.Fetch(settings.Latitude, settings.Longitude);
                    lastError = null;
                    Log.Write(string.Format(CultureInfo.InvariantCulture,
                        "weather for {0}: code {1}, {2}°C, wind {3} km/h, sunrise {4:HH:mm}, sunset {5:HH:mm}",
                        settings.City, weather.Code, weather.TemperatureC, weather.WindKmh, weather.Sunrise, weather.Sunset));
                }
                catch (Exception ex)
                {
                    // Keep the last weather; try again on the next tick.
                    lastError = "No connection: " + ex.Message;
                    Log.Write("weather fetch failed: " + ex);
                }
            }
            Apply();
        }

        void Apply()
        {
            if (weather != null)
            {
                var slot = Scenes.SlotFor(weather, weather.CityNow);
                var occasion = CurrentOccasion();
                // An occasion image, when the pack has one, replaces every weather image that day.
                var path = occasion == null ? null : Scenes.ResolveOccasion(settings.PackFolder, occasion, slot, weather.CityNow);
                var scene = path != null ? "occasion_" + occasion : CurrentScene();
                if (path == null) path = Scenes.Resolve(settings.PackFolder, scene, slot, weather.CityNow);
                if (path == null)
                {
                    lastError = "No images in " + settings.PackFolder;
                    Log.Write(lastError);
                }
                else if (path != currentPath)
                {
                    try
                    {
                        Wallpaper.Set(path);
                        currentPath = path;
                        Log.Write("wallpaper: " + scene + "/" + slot + " -> " + path);
                    }
                    catch (Exception ex)
                    {
                        lastError = "Could not set wallpaper: " + ex.Message;
                        Log.Write("set wallpaper failed: " + ex);
                    }
                }
            }
            UpdateMenu();
        }

        void ChooseCity()
        {
            using (var form = new CityForm(settings.City))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                settings.City = form.Selected.Label;
                settings.Latitude = form.Selected.Latitude;
                settings.Longitude = form.Selected.Longitude;
                settings.Save();
            }
            Tick(true);
        }

        void ChoosePack()
        {
            using (var dlg = new FolderBrowserDialog { SelectedPath = settings.PackFolder, Description = "Folder with <scene>_<slot>.jpg images" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                settings.PackFolder = dlg.SelectedPath;
                settings.Save();
            }
            currentPath = null;
            Apply();
        }

        void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            // Raised on a system thread; run the refresh on the UI thread.
            if (e.Mode == PowerModes.Resume) ui.Post(delegate { Tick(true); }, null);
        }

        protected override void ExitThreadCore()
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            timer.Stop();
            updateTimer.Stop();
            tray.Visible = false;
            tray.Dispose();
            base.ExitThreadCore();
        }
    }
}
