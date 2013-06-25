/*
 * Create Date：2010/10/22
 * Update Date：2011/04/22
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Security.Cryptography;
using FISCA.DSAClient.HttpUtil;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 處理 Envelope 事件。
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void EnvelopeProcessEventHandler(object sender, EnvelopeProcessEventArgs e);

    /// <summary>
    /// EnvelopeProcess 事件參數。
    /// </summary>
    public class EnvelopeProcessEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="rsp"></param>
        public EnvelopeProcessEventArgs(Envelope req, Envelope rsp)
        {
            Request = req;
            Response = rsp;
        }

        /// <summary>
        /// 
        /// </summary>
        public Envelope Request { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public Envelope Response { get; private set; }
    }

    /// <summary>
    /// 代表 DSA 的連線，可傳送 Request 文件到 DSA Server 。
    /// </summary>
    [Serializable()]
    public class Connection
    {
        #region DefaultHttpManager
        /// <summary>
        /// 預設的 HttpManager，所有的 Connection 都預設使用此 HttpManager。
        /// </summary>
        public static HttpManager DefaultHttpManager { get; private set; }
        static Connection()
        {
            DefaultHttpManager = new HttpManager();
        }
        #endregion

        /// <summary>
        /// Connect Service Name。
        /// </summary>
        protected static string ConnectService = "DS.Base.Connect";

        /// <summary>
        /// 建立 Connection 物件的執行個體。
        /// </summary>
        public Connection()
        {
            InitConnection();
        }

        /// <summary>
        /// 連線到 DSA。
        /// </summary>
        /// <param name="accessPoint">存取點位置，可以是 DSNS 名稱或是實體 Url。</param>
        /// <param name="targetContract">Contract 名稱，如果指定空串字會連到預設 Contract。</param>
        /// <param name="userName">使用者名稱。</param>
        /// <param name="password">密碼。</param>
        public void Connect(string accessPoint, string targetContract, string userName, string password)
        {
            Connect(AccessPoint.Parse(accessPoint), targetContract, new BasicToken(userName, password));
        }

        /// <summary>
        /// 使用 Passport 機制連線到 DSA。
        /// </summary>
        /// <param name="accessPoint">要連線的 DSA 位置。</param>
        /// <param name="targetContract">要連線的 Contract 名稱。</param>
        /// <param name="passportProvider">Passport 提供者 DSA 位置。</param>
        /// <param name="providerContract">Passport 提供者的 Contract 名稱。</param>
        /// <param name="userName">使用者名稱。</param>
        /// <param name="password">密碼</param>
        public void Connect(string accessPoint, string targetContract,
            string passportProvider, string providerContract,
            string userName, string password)
        {
            Connect(accessPoint, targetContract, passportProvider, providerContract,
                new BasicToken(userName, password));
        }

        /// <summary>
        /// 連線到 DSA。
        /// </summary>
        /// <param name="accessPoint">存取點位置，可以是 DSNS 名稱或是實體 Url。</param>
        /// <param name="targetContract">Contract 名稱，如果指定空串字會連到預設 Contract。</param>
        /// <param name="securityToken">安全代符。</param>
        public void Connect(string accessPoint, string targetContract, SecurityToken securityToken)
        {
            Connect(AccessPoint.Parse(accessPoint), targetContract, securityToken);
        }

        /// <summary>
        /// 使用 Passport 機制連線到 DSA。
        /// </summary>
        /// <param name="accessPoint">要連線的 DSA 位置。</param>
        /// <param name="targetContract">要連線的 Contract 名稱。</param>
        /// <param name="passportProvider">Passport 提供者 DSA 位置。</param>
        /// <param name="providerContract">Passport 提供者的 Contract 名稱。</param>
        /// <param name="securityToken">安全代符。</param>
        public void Connect(string accessPoint, string targetContract,
            string passportProvider, string providerContract, SecurityToken securityToken)
        {
            SecurityToken token = PassportToken.GetPassport(passportProvider, providerContract, securityToken);
            Connect(AccessPoint.Parse(accessPoint), targetContract, token);
        }

        /// <summary>
        /// 連線到 DSA。
        /// </summary>
        /// <param name="accessPoint">存取點位置。</param>
        /// <param name="targetContract">Contract 名稱，如果指定空串字會連到預設 Contract。</param>
        /// <param name="securityToken">安全代符。</param>
        public void Connect(AccessPoint accessPoint, string targetContract, SecurityToken securityToken)
        {
            AccessPoint = accessPoint;
            TargetContract = targetContract + "";
            SecurityToken = securityToken;

            Envelope req = new Envelope(new XmlStringHolder(EnableSession ? "<RequestSessionID/>" : ""));

            if (EnableSecureTunnel)
                req.TransportBySecureTunnel = true;

            OnConnecting = true;
            Envelope rsp = SendRequest(ConnectService, req);
            OnConnecting = false;

            if (EnableSession)
            {
                SessionToken session = new SessionToken(rsp.Body.XmlString, securityToken);
                SecurityToken = session;
            }

            try
            {
                if (rsp.Headers.Contains("Version"))
                    ServerVersion = new Version(new XmlHelper(rsp.Headers["Version"]).GetText("."));
                else
                    DefaultVersion();
            }
            catch
            {
                DefaultVersion();
            }

            if (rsp.Headers.Contains("UserInfo") && string.IsNullOrEmpty(TargetContract))
            {
                XmlHelper header = XmlHelper.ParseAsHelper(rsp.Headers["UserInfo"]);
                TargetContract = header.GetText("@Contract");
            }

            IsConnected = true;
        }

        /// <summary>
        /// 使用目前的 SecurityToken 切換到指定的 Contract。
        /// </summary>
        /// <param name="name">Contract 名稱。</param>
        /// <returns>已切換到新 Contract 的 Connection 物件。</returns>
        public Connection AsContract(string name)
        {
            return AsContract(name, SecurityToken);
        }

        /// <summary>
        ///  切換到指定的 Contract。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="securityToken"></param>
        /// <returns></returns>
        public Connection AsContract(string name, SecurityToken securityToken)
        {
            if (!IsConnected)
                throw new InvalidOperationException("請使用 Connect 方法進行連線。");

            Connection conn = this.MemberwiseClone() as Connection;

            if (securityToken != SecurityToken)
            {
                conn.TargetContract = name;
                conn.SecurityToken = securityToken;
            }
            else
                conn.TargetContract = name;

            //不同 Contract 時，安全通道需要重新建立。
            //因為 Contract KeyPair 不同。
            conn.STS = null;

            //測試是否可以正確連入。
            Envelope rsp = conn.SendRequest(ConnectService, new Envelope());

            if (rsp.Headers.Contains("UserInfo") && string.IsNullOrEmpty(TargetContract))
            {
                XmlHelper header = XmlHelper.ParseAsHelper(rsp.Headers["UserInfo"]);
                conn.TargetContract = header.GetText("@Contract");
            }

            return conn;
        }

        /// <summary>
        /// 開始一個 Batch 呼叫。
        /// </summary>
        /// <returns></returns>
        public BatchManager NewBatchManager()
        {
            return new BatchManager(this);
        }

        /// <summary>
        /// 傳送 DSA Request。
        /// </summary>
        /// <param name="srvName">服務名稱。</param>
        /// <param name="requestBodyContent">Request 資料。</param>
        /// <returns></returns>
        public IXmlable SendRequest(string srvName, IXmlable requestBodyContent)
        {
            return SendRequest(srvName, new Envelope(requestBodyContent), null).Body;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="srvName"></param>
        /// <param name="requestBodyContent"></param>
        /// <param name="batch">指定 BatchManager 之後，此 Request 的資料並不會直接寫入資料庫，直到 BatchManager.Complete() 被呼叫為此。</param>
        /// <returns></returns>
        public IXmlable SendRequest(string srvName, IXmlable requestBodyContent, BatchManager batch)
        {
            return SendRequest(srvName, new Envelope(requestBodyContent), batch).Body;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="srvName"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Envelope SendRequest(string srvName, Envelope request)
        {
            return SendRequest(srvName, request, null);
        }

        /// <summary>
        /// 傳送 DSA Request。
        /// </summary>
        /// <param name="srvName">服務名稱。</param>
        /// <param name="request">Request 資料。</param>
        /// <param name="batch">指定 BatchManager 之後，此 Request 的資料並不會直接寫入資料庫，直到 BatchManager.Complete() 被呼叫為此。</param>
        /// <returns></returns>
        public Envelope SendRequest(string srvName, Envelope request, BatchManager batch)
        {
            //將 Request 所必須的 Header 加入到 Envelope  中。
            PreprocessHeader(request, TargetContract, srvName, SecurityToken);

            SecureTunnel tunnel = SecureTunnel.TransparentTunnel; //透明通道，不會進行加密動作。

            //跟據設定值決定要不要使用加密通道。
            if (EnableSecureTunnel && !request.TransportBySecureTunnel.HasValue)
                tunnel = InitialSecureTunnel().NewTunnel();
            else if (request.TransportBySecureTunnel.HasValue && request.TransportBySecureTunnel.Value)
                tunnel = InitialSecureTunnel().NewTunnel();

            return SendRequest(TargetUrl, request, tunnel, OnConnectPreprocess, OnConnectPostprocess, Http, batch, this);
        }

        /// <summary>
        /// 直接傳送 Request 到目標 DSA，不經過 DS.Base.Connect 連線測試。
        /// </summary>
        /// <param name="accessPoint">目標 DSA 位置。</param>
        /// <param name="targetContract">目標 Contract。</param>
        /// <param name="srvName">服務名稱。</param>
        /// <param name="requestBodyContent">申請文件。</param>
        /// <param name="securityToken">安全代符。</param>
        /// <param name="transportBySTS">指示是否使用加密通道傳送資料。</param>
        /// <returns></returns>
        public static IXmlable SendRequest(string accessPoint, string targetContract, string srvName, IXmlable requestBodyContent, SecurityToken securityToken, bool transportBySTS)
        {
            return SendRequest(accessPoint, targetContract, srvName, new Envelope(requestBodyContent), securityToken, transportBySTS);
        }

        /// <summary>
        /// 直接傳送 Request 到目標 DSA，不經過 DS.Base.Connect 連線測試。
        /// </summary>
        /// <param name="accessPoint">目標 DSA 位置。</param>
        /// <param name="targetContract">目標 Contract。</param>
        /// <param name="srvName">服務名稱。</param>
        /// <param name="request">申請文件。</param>
        /// <param name="securityToken">安全代符。</param>
        /// <param name="transportBySTS">指示是否使用加密通道傳送資料。</param>
        /// <returns></returns>
        public static Envelope SendRequest(string accessPoint, string targetContract, string srvName, Envelope request, SecurityToken securityToken, bool transportBySTS)
        {
            //將 Request 所必須的 Header 加入到 Envelope  中。
            PreprocessHeader(request, targetContract, srvName, securityToken);

            SecureTunnel tunnel = SecureTunnel.TransparentTunnel;

            if (transportBySTS)
            {
                SecureTunnelService STS = new SecureTunnelService(Connection.DefaultHttpManager);
                STS.Init(AccessPoint.Parse(accessPoint), targetContract);
                tunnel = STS.NewTunnel();
            }

            return SendRequest(accessPoint, request, tunnel, null, null, Connection.DefaultHttpManager, null, null);
        }

        /// <summary>
        /// 傳送 DSA Request。
        /// </summary>
        internal static Envelope SendRequest(string target, Envelope request, SecureTunnel tunnel,
            Action<Envelope, Envelope> preprocess, Action<Envelope, Envelope> postprocess,
            HttpManager http, BatchManager batch, Connection identify)
        {
            Envelope srequest = null; //可能加密過的 Envelope。

            //如果有指定 Batch 則將相關資訊加入到 Header 中。
            if (batch != null)
            {
                if (batch._conn != identify)
                    throw new ArgumentException("BatchManager 必須使用在建立的 Connection 上。");

                batch.AddBatchHeader(request);
            }

            //連線前處理。
            if (preprocess != null)
                preprocess(request, null);

            srequest = tunnel.Protect(request);

            byte[] binaryrsp = http.Send(target, Encoding.UTF8.GetBytes(srequest.XmlString));

            Envelope response = Envelope.Parse(Encoding.UTF8.GetString(binaryrsp));
            Envelope sresponse = tunnel.Unprotect(response);

            if (!sresponse.Headers.Contains(Envelope.StatusElement))
                throw new EnvelopeSpecificationException("缺少「Status」元素。", null, sresponse.XmlString);

            XmlHelper elmStatus = XmlHelper.ParseAsHelper(sresponse.Headers[Envelope.StatusElement]);
            string code = elmStatus.GetText("Code");
            string message = elmStatus.GetText("Message");

            if (code.Trim() != "0")
            {
                DSAServerException dsaexp = new DSAServerException(code, message, request.XmlString, sresponse.XmlString, null);
                dsaexp.AccessPoint = target;

                throw dsaexp;
            }

            //連線後處理。
            if (postprocess != null)
                postprocess(request, sresponse);

            return sresponse;
        }

        /// <summary>
        /// 取得或設定是否啟用 Session 機制，以加速連線速度與提高安全性。
        /// </summary>
        public bool EnableSession { get; set; }

        /// <summary>
        /// 取得或設定是否啟用安全資料通道機制，此機制使用公開金鑰(PKI)技術達成。
        /// </summary>
        public bool EnableSecureTunnel { get; set; }

        /// <summary>
        /// 取得或設定 HttpManager，如果此屬性保持 Null，會自動使用 Connection.DefaultHttpManager。
        /// </summary>
        public HttpManager HttpManager { get; set; }

        /// <summary>
        /// 取得傳送資料的 HttpManager 物件，此屬性會依據 HttpManager 屬性的設定而自動選擇正確的物件。
        /// </summary>
        protected HttpManager Http
        {
            get
            {
                if (HttpManager == null)
                    return DefaultHttpManager;
                else
                    return HttpManager;
            }
        }

        /// <summary>
        /// 取得 DSA Server 的版本。
        /// </summary>
        public Version ServerVersion { get; private set; }

        /// <summary>
        /// 取得已連線的 DSA 存取點位置。
        /// </summary>
        public AccessPoint AccessPoint { get; private set; }

        /// <summary>
        /// 取得安全代符資訊。
        /// </summary>
        public SecurityToken SecurityToken { get; private set; }

        /// <summary>
        /// 取得 Request 傳送的目標 Contract。
        /// </summary>
        public string TargetContract { get; private set; }

        /// <summary>
        /// 取得是否已經連線。
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// 當正在連線時。
        /// </summary>
        protected bool OnConnecting { get; set; }

        /// <summary>
        /// 在 Connection 連線之前。
        /// </summary>
        public event EnvelopeProcessEventHandler ConnectPreprocess;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="rsp"></param>
        protected void OnConnectPreprocess(Envelope req, Envelope rsp)
        {
            if (!OnConnecting) return;

            if (ConnectPreprocess != null)
                ConnectPreprocess(this, new EnvelopeProcessEventArgs(req, rsp));
        }

        /// <summary>
        /// 在 Connection 連線之後。
        /// </summary>
        public event EnvelopeProcessEventHandler ConnectPostprocess;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="rsp"></param>
        protected void OnConnectPostprocess(Envelope req, Envelope rsp)
        {
            if (!OnConnecting) return;

            if (ConnectPostprocess != null)
                ConnectPostprocess(this, new EnvelopeProcessEventArgs(req, rsp));
        }

        /// <summary>
        /// 取得最後要傳送資料的 Url。
        /// </summary>
        protected string TargetUrl
        {
            get { return Path.Combine(AccessPoint.Url, TargetContract).Replace('\\', '/'); }
        }

        /// <summary>
        /// 重新設定所有屬性到預設值。
        /// </summary>
        private void InitConnection()
        {
            IsConnected = false;
            OnConnecting = false;
            AccessPoint = null;
            TargetContract = string.Empty;
            SecurityToken = null;
            HttpManager = null; //Null 代表使用 DefaultHttpManager。
            EnableSession = true;
            EnableSecureTunnel = false;
            STS = null;
            DefaultVersion();
        }

        private void DefaultVersion()
        {
            ServerVersion = new Version(3, 0);
        }

        /// <summary>
        /// 安全通道服務。
        /// </summary>
        private SecureTunnelService STS { get; set; }

        /// <summary>
        /// 執行緒同步物件。
        /// </summary>
        private object SyncRoot = new object();

        private SecureTunnelService InitialSecureTunnel()
        {
            lock (SyncRoot)
            {
                if (STS == null)
                {
                    STS = new SecureTunnelService(Http);
                    STS.Init(AccessPoint, TargetContract);
                }
                return STS;
            }
        }

        private static void PreprocessHeader(Envelope request, string contract, string srvName, SecurityToken securityToken)
        {
            //指定 TargetService
            request.Headers.Set(Envelope.TargetServiceElement, srvName);

            //指定 TargetContract
            request.Headers.Set(Envelope.TargetContractElement, contract);

            //指定 SecurityToken
            request.Headers.Set(securityToken.XmlString);
        }
    }
}
