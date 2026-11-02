using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WeatherWall
{
    public static class Wallpaper
    {
        const int SPI_SETDESKWALLPAPER = 0x0014;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SystemParametersInfo(int uiAction, int uiParam, string pvParam, int fWinIni);

        public static void Set(string path)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
                // 10 = Fill: covers the screen, keeps the aspect ratio.
                key.SetValue("WallpaperStyle", "10");
                key.SetValue("TileWallpaper", "0");
            }
            if (!SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public static class Autostart
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string Name = "WeatherWall";

        public static bool IsOn
        {
            get
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
                    return key != null && key.GetValue(Name) != null;
            }
        }

        public static void Set(bool on, string exePath)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (on) key.SetValue(Name, "\"" + exePath + "\"");
                else key.DeleteValue(Name, false);
            }
        }
    }
}
