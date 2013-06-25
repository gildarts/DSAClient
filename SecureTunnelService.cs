/*
 * Create Date：2011/01/14
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using FISCA.DSAClient.HttpUtil;
using System.IO;
using System.Xml;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 
    /// </summary>
    public class SecureTunnelService
    {
        private CriticalKey key = null;

        private HttpManager Http { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="http"></param>
        public SecureTunnelService(HttpManager http)
        {
            Http = http;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accessPoint"></param>
        /// <param name="targetContract"></param>
        public void Init(AccessPoint accessPoint, string targetContract)
        {
            try
            {
                Envelope req = new Envelope();
                req.Headers.Set(Envelope.TargetContractElement, "info");
                req.Headers.Set(Envelope.TargetServiceElement, "Public.GetPublicKey");

                string bodyContent = string.Format("<Content><Contract>{0}</Contract><Format>pkcs8</Format></Content>", targetContract);
                req.Body = new XmlStringHolder(bodyContent);

                string targetUrl = accessPoint.Url;

                byte[] binaryrsp = Http.Send(targetUrl, Encoding.UTF8.GetBytes(req.XmlString));

                RSACryptoServiceProvider serverUk = new RSACryptoServiceProvider();
                RSACryptoServiceProvider clientPk = new RSACryptoServiceProvider(1024);
                serverUk.FromXmlString(DecodePublicKey(binaryrsp));
                key = new CriticalKey(serverUk, clientPk);
            }
            catch (Exception ex)
            {
                throw new SecureTunnelException("建立安全通道時發生錯誤。", ex);
            }
        }

        private static string DecodePublicKey(byte[] binaryrsp)
        {
            string content = Encoding.UTF8.GetString(binaryrsp);

            if (!content.EndsWith("</Envelope>"))
                return content;
            else
            {
                Envelope rsp = Envelope.Parse(content);
                if (!rsp.Headers.Contains("Status"))
                    throw new DSAServerException("連線的 DSA 版本過於老舊。");

                string status = rsp.Headers["Status"];
                XmlHelper helper = new XmlHelper(status);

                DSAServerException srvExp = new DSAServerException(
                    helper.GetElement("Code").InnerText,
                    helper.GetElement("Message").InnerText,
                    string.Empty,
                    content, null);

                throw srvExp;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public SecureTunnel NewTunnel()
        {
            return new SecureTunnel(key);
        }

        internal class CriticalKey
        {
            public CriticalKey(RSACryptoServiceProvider serverUk, RSACryptoServiceProvider clientPk)
            {
                ServerPublicKey = serverUk;
                ClientPrivateKey = clientPk;
            }

            public RSACryptoServiceProvider ServerPublicKey { get; set; }

            public RSACryptoServiceProvider ClientPrivateKey { get; set; }

            private string _clientPublicKeyString = null;
            public string ClientPublicKeyString
            {
                get
                {
                    if (string.IsNullOrEmpty(_clientPublicKeyString))
                        _clientPublicKeyString = ClientPrivateKey.ToXmlString(false);

                    return _clientPublicKeyString;
                }
            }

            /// <summary>
            /// 用於執行緒同步。
            /// </summary>
            public object SyncRoot = new object();
        }
    }
}
