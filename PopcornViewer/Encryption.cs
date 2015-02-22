using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace PopcornViewer
{
    partial class MainForm
    {
        const string KEY = "tqaofoomeuyrypnrqtnqiiffjppblpkm";
        const string KEYII = "mgnahaftsmeayact";

        public static string Encrypt(string PlainText)
        {
            byte[] PlainBytes = Encoding.UTF8.GetBytes(PlainText);

            System.Security.Cryptography.SymmetricAlgorithm Alg = System.Security.Cryptography.SymmetricAlgorithm.Create();

            MemoryStream MemStr = new MemoryStream();
            byte[] KeyBytes = Encoding.ASCII.GetBytes(KEY);
            byte[] KeyIIBytes = Encoding.ASCII.GetBytes(KEYII);

            CryptoStream CryptStr = new CryptoStream(MemStr, Alg.CreateEncryptor(KeyBytes, KeyIIBytes), CryptoStreamMode.Write);

            CryptStr.Write(PlainBytes, 0, PlainText.Length);
            CryptStr.Close();

            return Convert.ToBase64String(MemStr.ToArray());
        }

        private string Decrypt(string EncryptedText)
        {
            byte[] EncryptedBytes;
            try { EncryptedBytes = Convert.FromBase64String(EncryptedText.Substring(0, EncryptedText.IndexOf('$'))); }
            catch { return ""; }

            System.Security.Cryptography.SymmetricAlgorithm Alg = System.Security.Cryptography.SymmetricAlgorithm.Create();

            MemoryStream MemStr = new MemoryStream();
            byte[] KeyBytes = Encoding.ASCII.GetBytes(KEY);
            byte[] KeyIIBytes = Encoding.ASCII.GetBytes(KEYII);

            CryptoStream CryptStr = new CryptoStream(MemStr, Alg.CreateDecryptor(KeyBytes, KeyIIBytes), CryptoStreamMode.Write);

            CryptStr.Write(EncryptedBytes, 0, EncryptedBytes.Length);
            CryptStr.Close();

            return Encoding.UTF8.GetString(MemStr.ToArray());
        }
    }
}
