using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace WeatherWall
{
    static class Program
    {
        // Where WeatherWall lives once installed: per user, no admin rights, writable for updates.
        public static string InstallDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "WeatherWall"); }
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (!args.Contains("--portable") && InstallAndRelaunch()) return;

            bool isFirst;
            using (var mutex = new Mutex(true, "WeatherWall.SingleInstance", out isFirst))
            using (var showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, "WeatherWall.ShowWindow"))
            {
                // Right after a self-update the old process is still closing; wait for it.
                if (!isFirst && args.Contains("--updated")) isFirst = WaitFor(mutex);
                if (!isFirst)
                {
                    // Already running (a shortcut, or the exe opened again): bring up its window.
                    showSignal.Set();
                    return;
                }
                // Log every failure instead of a dialog or a silent exit, and keep running.
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => Log.Write("unexpected error: " + e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                    Log.Write("fatal error (exiting): " + e.ExceptionObject);
                Updater.CleanUpOldExe();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApp(args.Contains("--updated"), args.Contains("--show"), showSignal));
            }
        }

        public static bool IsInstalledCopy
        {
            get
            {
                return string.Equals(Path.GetFullPath(Application.ExecutablePath),
                    Path.GetFullPath(Path.Combine(InstallDir, "WeatherWall.exe")), StringComparison.OrdinalIgnoreCase);
            }
        }

        // Run from anywhere else (Downloads, a USB stick): copy into InstallDir and start that copy,
        // so the start-up entry and the updater always point at one stable place.
        // Returns true when this process should just exit.
        static bool InstallAndRelaunch()
        {
            var running = Process.GetCurrentProcess().MainModule.FileName;
            var installed = Path.Combine(InstallDir, "WeatherWall.exe");
            if (string.Equals(Path.GetFullPath(running), Path.GetFullPath(installed), StringComparison.OrdinalIgnoreCase))
                return false;
            try
            {
                Directory.CreateDirectory(InstallDir);
                if (!File.Exists(installed) || IsOlder(installed, running)) File.Copy(running, installed, true);
            }
            catch (IOException)
            {
                // The installed copy is running and locked: it is already installed, just let it be.
            }
            // The user opened this copy on purpose: show the window, not just a tray icon.
            Process.Start(installed, "--show");
            return true;
        }

        static bool IsOlder(string candidate, string than)
        {
            return new Version(FileVersionInfo.GetVersionInfo(candidate).FileVersion)
                < new Version(FileVersionInfo.GetVersionInfo(than).FileVersion);
        }

        static bool WaitFor(Mutex mutex)
        {
            try { return mutex.WaitOne(TimeSpan.FromSeconds(20)); }
            catch (AbandonedMutexException) { return true; }
        }
    }
}
