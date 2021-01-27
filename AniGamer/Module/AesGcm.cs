using System;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Module
{
    public class AesGcm256
    {
        // Pre-configured Encryption Parameters
        public static readonly int NonceBitSize = 128;
        public static readonly int MacBitSize = 128;
        public static readonly int KeyBitSize = 256;

        private AesGcm256() { }


        public static string ChromeCookies(byte[] Date, byte[] Key)
        {
            
            try
            {
                byte[] VI = Date.Skip(3).Take(12).ToArray();
                string Content = System.Convert.ToBase64String(Date.Skip(15).ToArray());
                return AesGcm256.Decrypt(Content, Key, VI);
            }
            catch (Exception ex)
            {
                WPFMessageBox.Show(ex.Message);
            }

            return "";
        }

        public static string Decrypt(string EncryptedText, byte[] key, byte[] iv)
        {
            string sR = string.Empty;
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(EncryptedText);

                GcmBlockCipher cipher = new GcmBlockCipher(new AesFastEngine());
                AeadParameters parameters =
                          new AeadParameters(new KeyParameter(key), 128, iv, null);
                //ParametersWithIV parameters = new ParametersWithIV(new KeyParameter(key), iv);

                cipher.Init(false, parameters);
                byte[] plainBytes = new byte[cipher.GetOutputSize(encryptedBytes.Length)];
                Int32 retLen = cipher.ProcessBytes
                               (encryptedBytes, 0, encryptedBytes.Length, plainBytes, 0);
                cipher.DoFinal(plainBytes, retLen);

                sR = Encoding.UTF8.GetString(plainBytes).TrimEnd("\r\n\0".ToCharArray());
            }
            catch (Exception ex)
            {
                WPFMessageBox.Show(ex.Message);
            }

            return sR;
        }
    }
}