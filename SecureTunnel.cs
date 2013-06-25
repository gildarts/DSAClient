/*
 * Create Date：2011/01/14
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 
    /// </summary>
    public class SecureTunnel
    {
        /// <summary>
        /// 
        /// </summary>
        public static SecureTunnel TransparentTunnel = new TransparentTunnel();

        private SecureTunnelService.CriticalKey Key = null;

        private AesManaged SecretKey = new AesManaged();
        private string SecretKeyString = Guid.NewGuid().ToString();

        /// <summary>
        /// 
        /// </summary>
        internal SecureTunnel(SecureTunnelService.CriticalKey key)
        {
            this.Key = key;
            SecretKey.Mode = CipherMode.ECB;
            SecretKey.Padding = PaddingMode.PKCS7;
            SecretKey.Key = MD5Password(SecretKeyString);
        }

        private static byte[] MD5Password(string password)
        {
            int t1 = Environment.TickCount;
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] fromData = UTF8Encoding.UTF8.GetBytes(password);
            byte[] targetData = md5.ComputeHash(fromData);

            //Console.WriteLine((Environment.TickCount - t1).ToString());
            return targetData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public virtual Envelope Protect(Envelope env)
        {
            Envelope cipherEnv = new Envelope();
            cipherEnv.Headers.Set(XmlHelper.ParseAsHelper(env.Headers[Envelope.TargetContractElement]));

            XmlHelper cryptoToken = new XmlHelper("<CryptoToken/>");
            cryptoToken.SetAttribute(".", "Type", "Static");

            string cipherSecretKey;

            lock (Key.SyncRoot) //當要使用 ServerPublicKey 時進行同步。
            {
                cipherSecretKey = Convert.ToBase64String(Key.ServerPublicKey.Encrypt(Encoding.UTF8.GetBytes(SecretKeyString), false));
            }

            string cipherPublicKey = EncryptoPublicKey();
            string cipherContent = EncryptoContent(env);

            cryptoToken.AddElement(".", "SecretKey", cipherSecretKey);
            cryptoToken.AddElement(".", "PublicKey", cipherPublicKey);
            cryptoToken.AddElement(".", "Cipher", cipherContent);

            cipherEnv.Headers.Set(cryptoToken);

            return cipherEnv;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public virtual Envelope Unprotect(Envelope env)
        {
            XmlHelper rsp = new XmlHelper(env.Headers["CryptoToken"]);
            string b64Cipher = rsp.GetElement("Cipher").InnerText;

            using (ICryptoTransform crypto = SecretKey.CreateDecryptor())
            {
                byte[] binary = Convert.FromBase64String(b64Cipher);
                binary = crypto.TransformFinalBlock(binary, 0, binary.Length);

                return Envelope.Parse(Encoding.UTF8.GetString(binary));
            }
        }

        private string EncryptoContent(Envelope env)
        {
            using (ICryptoTransform crypto = SecretKey.CreateEncryptor())
            {
                byte[] binaryEnv = Encoding.UTF8.GetBytes(env.XmlString);
                return Convert.ToBase64String(crypto.TransformFinalBlock(binaryEnv, 0, binaryEnv.Length));
            }
        }

        private string EncryptoPublicKey()
        {
            using (ICryptoTransform crypto = SecretKey.CreateEncryptor())
            {
                string PlainUk = Key.ClientPublicKeyString;
                byte[] binaryUk = Encoding.UTF8.GetBytes(PlainUk);
                return Convert.ToBase64String(crypto.TransformFinalBlock(binaryUk, 0, binaryUk.Length));
            }
        }
    }

    internal class TransparentTunnel : SecureTunnel
    {
        public TransparentTunnel()
            : base(null)
        {
        }

        public override Envelope Protect(Envelope env)
        {
            return env;
        }

        public override Envelope Unprotect(Envelope env)
        {
            return env;
        }
    }
}
