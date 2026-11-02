using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WeatherWall
{
    // Start menu and desktop shortcuts. They open the WeatherWall window (--show), which also reaches
    // a copy that is already running.
    public static class Shortcuts
    {
        static string DesktopLink
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "WeatherWall.lnk"); }
        }

        static string StartMenuLink
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "WeatherWall.lnk"); }
        }

        public static bool OnDesktop { get { return File.Exists(DesktopLink); } }

        public static void SetDesktop(bool on, string exe) { Set(DesktopLink, on, exe); }

        public static void SetStartMenu(bool on, string exe) { Set(StartMenuLink, on, exe); }

        static void Set(string link, bool on, string exe)
        {
            if (!on)
            {
                if (File.Exists(link)) File.Delete(link);
                return;
            }
            // WScript.Shell ships with Windows; late-bound, so no interop assembly is needed.
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            var shell = Activator.CreateInstance(shellType);
            object shortcut = null;
            try
            {
                shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { link });
                var type = shortcut.GetType();
                Action<string, object> put = (name, value) =>
                    type.InvokeMember(name, BindingFlags.SetProperty, null, shortcut, new[] { value });
                put("TargetPath", exe);
                put("Arguments", "--show");
                put("WorkingDirectory", Path.GetDirectoryName(exe));
                put("IconLocation", exe + ",0");
                put("Description", "WeatherWall: your wallpaper follows the weather and the time of day");
                type.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            }
            finally
            {
                if (shortcut != null) Marshal.FinalReleaseComObject(shortcut);
                Marshal.FinalReleaseComObject(shell);
            }
        }
    }
}
