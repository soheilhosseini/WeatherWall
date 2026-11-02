using System;
using System.IO;
using System.Web.Script.Serialization;

namespace WeatherWall
{
    // Stored as JSON in %APPDATA%\WeatherWall\settings.json.
    public class Settings
    {
        // False (the default): City follows the Windows time zone. True: the user picked City.
        public bool CityIsManual { get; set; }
        // The Windows time zone the automatic City was found for; a change triggers a new lookup.
        public string AutoZone { get; set; }
        public string City { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string PackFolder { get; set; }
        // "auto", or a scene name to pin the wallpaper to that scene.
        public string SceneOverride { get; set; }
        public bool FirstRunDone { get; set; }
        // Signature of the pack.zip last installed by the updater.
        public string PackSignature { get; set; }

        internal static string Dir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WeatherWall"); }
        }

        static string FilePath
        {
            get { return Path.Combine(Dir, "settings.json"); }
        }

        public static Settings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var s = new JavaScriptSerializer().Deserialize<Settings>(File.ReadAllText(FilePath));
                    if (s != null) return s.WithDefaults();
                }
            }
            catch (Exception) { }
            return new Settings().WithDefaults();
        }

        Settings WithDefaults()
        {
            if (string.IsNullOrEmpty(City))
            {
                // Only until the time-zone city is found (or for zones that name no city).
                City = "Köthen (Anhalt), Germany";
                Latitude = 51.75185;
                Longitude = 11.97093;
            }
            if (string.IsNullOrEmpty(PackFolder))
                PackFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pack");
            if (string.IsNullOrEmpty(SceneOverride))
                SceneOverride = "auto";
            return this;
        }

        public void Save()
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, new JavaScriptSerializer().Serialize(this));
        }
    }

    // %APPDATA%\WeatherWall\log.txt, trimmed to the last ~200 KB.
    public static class Log
    {
        public static void Write(string line)
        {
            try
            {
                Directory.CreateDirectory(Settings.Dir);
                var path = Path.Combine(Settings.Dir, "log.txt");
                if (File.Exists(path) && new FileInfo(path).Length > 200 * 1024) File.Delete(path);
                File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line + Environment.NewLine);
            }
            catch (Exception) { }
        }
    }
}
