using System;
using System.IO;
using System.Security.Cryptography;

// Publisher-side signing for WeatherWall releases.
//   SignTool keygen          creates the key pair (once) and prints the public key XML
//   SignTool sign <file>     writes <file>.sig
//   SignTool verify <file>   checks <file>.sig against the stored public key
// The private key lives in %USERPROFILE%\.weatherwall\, outside the repository. Back it up:
// without it no installed copy will ever accept another update.
static class SignTool
{
    static string KeyDir
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".weatherwall"); }
    }

    static string PrivateKeyPath { get { return Path.Combine(KeyDir, "signing-key.private.xml"); } }
    static string PublicKeyPath { get { return Path.Combine(KeyDir, "signing-key.public.xml"); } }

    static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "keygen") return KeyGen();
        if (args.Length == 2 && args[0] == "sign") return Sign(args[1]);
        if (args.Length == 2 && args[0] == "verify") return Verify(args[1]);
        Console.Error.WriteLine("usage: SignTool keygen | sign <file> | verify <file>");
        return 2;
    }

    static RSACryptoServiceProvider NewRsa()
    {
        return new RSACryptoServiceProvider(3072) { PersistKeyInCsp = false };
    }

    static int KeyGen()
    {
        if (File.Exists(PrivateKeyPath))
        {
            Console.Error.WriteLine("A key already exists at " + PrivateKeyPath + "; refusing to overwrite it.");
            return 1;
        }
        Directory.CreateDirectory(KeyDir);
        using (var rsa = NewRsa())
        {
            File.WriteAllText(PrivateKeyPath, rsa.ToXmlString(true));
            File.WriteAllText(PublicKeyPath, rsa.ToXmlString(false));
            Console.WriteLine(rsa.ToXmlString(false));
        }
        return 0;
    }

    static int Sign(string file)
    {
        using (var rsa = NewRsa())
        {
            rsa.FromXmlString(File.ReadAllText(PrivateKeyPath));
            var sig = rsa.SignData(File.ReadAllBytes(file), CryptoConfig.MapNameToOID("SHA256"));
            File.WriteAllText(file + ".sig", Convert.ToBase64String(sig));
        }
        Console.WriteLine("signed " + file);
        return 0;
    }

    static int Verify(string file)
    {
        using (var rsa = NewRsa())
        {
            rsa.FromXmlString(File.ReadAllText(PublicKeyPath));
            var ok = rsa.VerifyData(File.ReadAllBytes(file), CryptoConfig.MapNameToOID("SHA256"),
                Convert.FromBase64String(File.ReadAllText(file + ".sig").Trim()));
            Console.WriteLine((ok ? "OK   " : "BAD  ") + file);
            return ok ? 0 : 1;
        }
    }
}
