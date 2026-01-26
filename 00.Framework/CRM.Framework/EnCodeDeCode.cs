using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CRM.Framework
{
    public class EnCodeDeCode
    {
        public static string EncodeServerName(string serverName)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(serverName));
        }

        public static string DecodeServerName(string encodedServername)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encodedServername));
        }

        public static string Encrypt(string textToEncrypt, string publickey, string privatekey)
        {
            try
            {
                byte[] privatekeyByte = { };
                privatekeyByte = Encoding.UTF8.GetBytes("natinikatimodern");
                byte[] publickeybyte = { };
                publickeybyte = Encoding.UTF8.GetBytes("atixnikatimodern");
                string ToReturn = "";
                MemoryStream ms = null;
                CryptoStream cs = null;
                byte[] inputbyteArray = Encoding.UTF8.GetBytes(textToEncrypt);
                using (Aes des = Aes.Create())
                {
                    ms = new MemoryStream();
                    cs = new CryptoStream(ms, des.CreateEncryptor(publickeybyte, privatekeyByte), CryptoStreamMode.Write);
                    cs.Write(inputbyteArray, 0, inputbyteArray.Length);
                    cs.FlushFinalBlock();
                    ToReturn = Convert.ToBase64String(ms.ToArray());
                }
                return ToReturn;
            }
            catch
            {
                throw new Exception();
            }
        }

        public static string Decrypt(string textToDecrypt)
        {
            try
            {
                byte[] privatekeyByte = { };
                privatekeyByte = Encoding.UTF8.GetBytes("natinikatimodern");
                byte[] publickeybyte = { };
                publickeybyte = Encoding.UTF8.GetBytes("atixnikatimodern");
                string ToReturn = "";
                MemoryStream ms = null;
                CryptoStream cs = null;
                byte[] inputbyteArray = new byte[textToDecrypt.Replace(" ", "+").Length];
                inputbyteArray = Convert.FromBase64String(textToDecrypt.Replace(" ", "+"));
                using (Aes des = Aes.Create())
                {
                    ms = new MemoryStream();
                    cs = new CryptoStream(ms, des.CreateDecryptor(publickeybyte, privatekeyByte), CryptoStreamMode.Write);
                    cs.Write(inputbyteArray, 0, inputbyteArray.Length);
                    cs.FlushFinalBlock();
                    Encoding encoding = Encoding.UTF8;
                    ToReturn = encoding.GetString(ms.ToArray());
                }
                return ToReturn;
            }
            catch (Exception ae)
            {
                throw new Exception(ae.Message, ae.InnerException);
            }
        }


    }
}
