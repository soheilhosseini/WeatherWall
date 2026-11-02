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
            {
                // Right after a self-update the old process is still closing; wait for it.
                if (!isFirst && args.Contains("--updated")) isFirst = WaitFor(mutex);
                if (!isFirst) return;
                Updater.CleanUpOldExe();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApp(args.Contains("--updated")));
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
            Process.Start(installed);
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
