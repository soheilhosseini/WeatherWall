using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace WeatherWall
{
    // Self-update from GitHub Releases. The app repo's latest release carries WeatherWall.exe, the
    // pack repo's latest release carries pack.zip, each with a .sig file. The images live in their
    // own repository so that losing it never takes the app or its updates down. Nothing is installed unless its signature verifies
    // against the public key below; the private key never leaves the publisher's machine.
    public static class Updater
    {
        public const string Owner = "soheilhosseini";
        public const string Repo = "WeatherWall";
        public const string PackRepo = "WeatherWall-Pack";

        public const string PublicKeyXml = "<RSAKeyValue><Modulus>y6aBIoZmGYfpdDdO382WZJk0cJGC9631pq/rnIiyAc5v7IaInieKmuz8QyyyAPHZYYwArIEhuGlopojtFRBMP2BRdpUNFWrIM7TVkg2aKP/aSXKM1dOIWNQ13d+nlBwOHwXjgUN1JuyLU3/F+iGzFN4hdnj3UiOeQgcne/6OUnnj0vSZ1fqvodhrBbXIX95Fr5wZmibLqQkKoQ7xM0zPY8n68JMN+q1nZX50UjfRse+KBQuwmIezlW2oAV1ZAhuCd44nqr7v9we1FyTnELlT2FVFUFF1VW/H9gnUpNlKTKF9ZULK5kbBBMUNOJcCrIrkjMjlG1eqrp16/kk7eAc0mS/JM6t3AU1iHmcEHYVFku3hzWXs6B/3MiqvxH4YaWBgCI3m47jniNCFG13rcaOcZML/IJnQ7HStmhIrZDnp34pEEspkcH7HWevQDKeBoWD+rQKWC/6Dyn/2SZAxDw9sFUg/1wXVNfw4ts20aTrCaS1XIzTEBPsrTij6MuLd4ry5</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public static Version CurrentVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public static string CurrentVersionText
        {
            get { var v = CurrentVersion; return v.Major + "." + v.Minor + "." + v.Build; }
        }

        // "v1.2.3" or "1.2.3" -> 1.2.3; anything else -> null.
        public static Version ParseTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            Version v;
            return Version.TryParse(tag.TrimStart('v', 'V'), out v) ? v : null;
        }

        public static bool IsNewer(string tag, Version current)
        {
            var v = ParseTag(tag);
            if (v == null) return false;
            var normalised = new Version(v.Major, v.Minor, Math.Max(v.Build, 0));
            var mine = new Version(current.Major, current.Minor, Math.Max(current.Build, 0));
            return normalised > mine;
        }

        public static bool VerifySignature(byte[] data, string signatureBase64, string publicKeyXml)
        {
            try
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(publicKeyXml);
                    return rsa.VerifyData(data, CryptoConfig.MapNameToOID("SHA256"),
                        Convert.FromBase64String(signatureBase64.Trim()));
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public class Release
        {
            public string Tag;
            public Dictionary<string, string> AssetUrls = new Dictionary<string, string>();
        }

        public static Release LatestRelease(string repo)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var url = "https://api.github.com/repos/" + Owner + "/" + repo + "/releases/latest";
            var root = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(DownloadString(url));
            var release = new Release { Tag = (string)root["tag_name"] };
            foreach (Dictionary<string, object> asset in (IList)root["assets"])
                release.AssetUrls[(string)asset["name"]] = (string)asset["browser_download_url"];
            return release;
        }

        static WebClient Client()
        {
            var wc = new WebClient { Encoding = Encoding.UTF8 };
            wc.Headers[HttpRequestHeader.UserAgent] = "WeatherWall/" + CurrentVersionText;
            return wc;
        }

        static string DownloadString(string url)
        {
            using (var wc = Client()) return wc.DownloadString(url);
        }

        // progress (0-100) is called from a pool thread while the file downloads.
        static byte[] DownloadBytes(string url, Action<int> progress = null)
        {
            using (var wc = Client())
            {
                if (progress != null) wc.DownloadProgressChanged += (s, e) => progress(e.ProgressPercentage);
                try { return wc.DownloadDataTaskAsync(new Uri(url)).Result; }
                catch (AggregateException ex) { throw ex.InnerException; }
            }
        }

        // Downloads name and name.sig from the release; returns the bytes only if the signature holds.
        static byte[] DownloadVerified(Release release, string name, out string signature, Action<int> progress = null)
        {
            signature = null;
            string url, sigUrl;
            if (!release.AssetUrls.TryGetValue(name, out url) || !release.AssetUrls.TryGetValue(name + ".sig", out sigUrl))
                return null;
            signature = DownloadString(sigUrl).Trim();
            var data = DownloadBytes(url, progress);
            if (!VerifySignature(data, signature, PublicKeyXml))
                throw new InvalidDataException(name + " from " + release.Tag + " failed its signature check; not installed.");
            return data;
        }

        // Returns the new pack signature when the pack was replaced, else null.
        public static string UpdatePackIfChanged(Release release, string packFolder, string installedSignature,
            Action<int> progress = null)
        {
            string sigUrl;
            if (!release.AssetUrls.TryGetValue("pack.zip.sig", out sigUrl)) return null;
            if (DownloadString(sigUrl).Trim() == installedSignature) return null;

            string signature;
            if (progress != null) progress(0);
            var zip = DownloadVerified(release, "pack.zip", out signature, progress);
            if (zip == null) return null;

            var staging = packFolder.TrimEnd('\\') + ".new";
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            Directory.CreateDirectory(staging);
            var zipPath = Path.Combine(staging, "pack.zip");
            File.WriteAllBytes(zipPath, zip);
            ZipFile.ExtractToDirectory(zipPath, Path.Combine(staging, "images"));

            Directory.CreateDirectory(packFolder);
            foreach (var old in Directory.GetFiles(packFolder)) File.Delete(old);
            foreach (var img in Directory.GetFiles(Path.Combine(staging, "images")))
                File.Move(img, Path.Combine(packFolder, Path.GetFileName(img)));
            Directory.Delete(staging, true);
            return signature;
        }

        // Puts the verified new exe in place of the running one. Windows lets a running exe be
        // renamed, so the old file becomes WeatherWall.exe.old and is deleted on the next start.
        // Returns true when the caller should restart.
        public static bool UpdateAppIfNewer(Release release)
        {
            if (!IsNewer(release.Tag, CurrentVersion)) return false;
            string signature;
            var exeBytes = DownloadVerified(release, "WeatherWall.exe", out signature);
            if (exeBytes == null) return false;

            var exe = Process.GetCurrentProcess().MainModule.FileName;
            var fresh = exe + ".new";
            var old = exe + ".old";
            File.WriteAllBytes(fresh, exeBytes);
            if (File.Exists(old)) File.Delete(old);
            File.Move(exe, old);
            File.Move(fresh, exe);
            return true;
        }

        public static void CleanUpOldExe()
        {
            try
            {
                var old = Process.GetCurrentProcess().MainModule.FileName + ".old";
                if (File.Exists(old)) File.Delete(old);
            }
            catch (Exception) { }
        }
    }
}
