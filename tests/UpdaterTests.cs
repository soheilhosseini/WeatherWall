using System;
using System.Security.Cryptography;
using System.Text;

namespace WeatherWall.Tests
{
    // Version comparison and signature checks: the two things that decide whether an update installs.
    static class UpdaterTests
    {
        static int failures;

        static void Check(bool ok, string what)
        {
            if (ok) return;
            failures++;
            Console.WriteLine("FAIL " + what);
        }

        public static int Run()
        {
            var current = new Version(1, 2, 3);
            Check(Updater.IsNewer("v1.2.4", current), "patch bump is newer");
            Check(Updater.IsNewer("v1.10.0", current), "1.10 is newer than 1.2 (numeric, not text)");
            Check(!Updater.IsNewer("v1.2.3", current), "same version is not newer");
            Check(!Updater.IsNewer("v1.2.2", current), "older version is not newer");
            Check(!Updater.IsNewer("latest", current), "a tag that is not a version never installs");
            Check(!Updater.IsNewer(null, current), "missing tag never installs");
            Check(Updater.IsNewer("2.0", current), "tag without v prefix");

            var data = Encoding.UTF8.GetBytes("pretend this is WeatherWall.exe");
            string publicKey, signature, otherSignature;
            using (var rsa = new RSACryptoServiceProvider(2048) { PersistKeyInCsp = false })
            {
                publicKey = rsa.ToXmlString(false);
                signature = Convert.ToBase64String(rsa.SignData(data, CryptoConfig.MapNameToOID("SHA256")));
            }
            using (var stranger = new RSACryptoServiceProvider(2048) { PersistKeyInCsp = false })
                otherSignature = Convert.ToBase64String(stranger.SignData(data, CryptoConfig.MapNameToOID("SHA256")));

            Check(Updater.VerifySignature(data, signature, publicKey), "genuine signature verifies");
            var tampered = (byte[])data.Clone();
            tampered[0] ^= 1;
            Check(!Updater.VerifySignature(tampered, signature, publicKey), "one changed byte fails");
            Check(!Updater.VerifySignature(data, otherSignature, publicKey), "signature from another key fails");
            Check(!Updater.VerifySignature(data, "not base64!", publicKey), "garbage signature fails, no crash");
            Check(!Updater.VerifySignature(data, signature, Updater.PublicKeyXml), "the shipped key rejects a test-key signature");
            Check(Updater.PublicKeyXml.Contains("<Modulus>") && !Updater.PublicKeyXml.Contains("<D>"),
                "the shipped key is a public key, never the private one");
            return failures;
        }
    }
}
