using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace WeatherWall
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
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

        static bool WaitFor(Mutex mutex)
        {
            try { return mutex.WaitOne(TimeSpan.FromSeconds(20)); }
            catch (AbandonedMutexException) { return true; }
        }
    }
}
