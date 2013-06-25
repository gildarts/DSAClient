/*
 * Create Date：2010/10/22
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FISCA.DSAClient.HttpUtil;
using System.IO;
using System.Xml;
using System.Threading;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 代表名稱解析邏輯封裝類別。
    /// </summary>
    public class NameResolver
    {
        #region DefaultHttpManager
        /// <summary>
        /// 預設的 HttpManager，所有的 NameResolver 都預設使用此 HttpManager，
        /// 變更此屬性，只會影響新的執行個體。
        /// </summary>
        public static HttpManager DefaultHttpManager { get; private set; }
        static NameResolver()
        {
            DefaultHttpManager = new HttpManager();
        }
        #endregion

        /// <summary>
        /// 取得名稱解析的執行緒同步物件。
        /// </summary>
        public ManualResetEvent WaitHandle { get; private set; }

        /// <summary>
        /// 管理 HTTP 的連線。
        /// </summary>
        public HttpManager HttpManager { get; set; }

        /// <summary>
        /// DSNS Server 實體位置。
        /// </summary>
        public string NameServer { get; private set; }

        /// <summary>
        /// 需要解析的 DSNS 名稱。
        /// </summary>
        public string NameToResolve { get; private set; }

        /// <summary>
        /// 解析結果。
        /// </summary>
        public string ResolveResult { get; private set; }

        /// <summary>
        /// 解析所發生的錯誤，如果沒有錯誤值為 Null。
        /// </summary>
        public Exception Error { get; private set; }

        /// <summary>
        /// 解析動作是否成功。
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        /// <param name="nameServer">DSNS Server 實體位置。</param>
        /// <param name="nameToResolve">要解析的 DSNS 名稱。</param>
        public NameResolver(string nameServer, string nameToResolve)
        {
            HttpManager = DefaultHttpManager;
            NameServer = nameServer;
            NameToResolve = nameToResolve;
            ResolveResult = string.Empty;
            Error = null;
            Success = false;
            WaitHandle = new ManualResetEvent(false);
        }

        /// <summary>
        /// 執行解析動作。
        /// </summary>
        public void Resolve()
        {
            try
            {
                Success = false;
                //Guid 是 LogID。
                string logid = Guid.NewGuid().ToString();
                string request = string.Format(CommonResources.NameResolveRequest, logid, NameToResolve);
                byte[] binaryreq = Encoding.UTF8.GetBytes(request);

                byte[] binaryrsp = HttpManager.Send(NameServer, binaryreq);

                using (Stream binary = new MemoryStream(binaryrsp))
                {
                    using (XmlReader reader = XmlReader.Create(new XmlTextReader(binary), XmlParsing.ReaderSettings))
                    {
                        if (reader.ReadToFollowing("Body"))
                        {
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.Element)
                                {
                                    ResolveResult = reader.ReadString();

                                    if (string.IsNullOrEmpty(ResolveResult))
                                        break; //直接跳出 while ，讓程式產生 Exception。

                                    Success = true;
                                    break;
                                }
                            }
                            if (string.IsNullOrEmpty(ResolveResult))
                                throw new DSNameResolveException("解析名稱失敗，未取得任何結果。", NameToResolve, NameServer, null);
                        }
                        else
                        {
                            using (StreamReader rspreader = new StreamReader(new MemoryStream(binaryrsp)))
                            {
                                throw new DSAProtocolException("DSA 回應的 Response 不正確。", request, rspreader.ReadToEnd(), NameServer, null);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error = ex;
            }
            finally
            {
                WaitHandle.Set();
            }
        }
    }
}
