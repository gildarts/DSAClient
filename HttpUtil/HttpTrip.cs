/*
 * Create Date：2010/10/22
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;

namespace FISCA.DSAClient.HttpUtil
{
    /// <summary>
    /// 代表 Http 目前狀態。
    /// </summary>
    public enum TripStatus
    {
        /// <summary>
        /// 正在等待執行。
        /// </summary>
        Padding,
        /// <summary>
        /// 正在執行。
        /// </summary>
        Running,
        /// <summary>
        /// 執行完成。
        /// </summary>
        Success,
        /// <summary>
        /// 工作被取消。
        /// </summary>
        Cancelled,
        /// <summary>
        /// 工作已失敗，執行重試。
        /// </summary>
        Retry,
        /// <summary>
        /// 執行失敗。
        /// </summary>
        Failure
    }

    /// <summary>
    /// 代表一個 Http 傳送/接收動作，每次 Request 都會建立新 Trip，包含 Retry 也會建立新的 Trip。
    /// </summary>
    [Serializable()]
    public sealed class HttpTrip
    {
        /// <summary>
        /// 取得 Request Http Header 資訊。
        /// </summary>
        public WebHeaderCollection RequestHeaders { get; internal set; }

        /// <summary>
        /// 取得 Response Http Header 資訊。
        /// </summary>
        public WebHeaderCollection ResponseHeaders { get; internal set; }

        /// <summary>
        /// Request Data。
        /// </summary>
        public byte[] Request { get; internal set; }

        /// <summary>
        /// Response Data。
        /// </summary>
        public byte[] Response { get; internal set; }

        /// <summary>
        /// Target Url。
        /// </summary>
        public string TargetUrl { get; internal set; }

        /// <summary>
        /// Manager。
        /// </summary>
        public HttpManager Manager { get; private set; }

        /// <summary>
        /// 取得傳送的位元組數目，-1代表未知狀態。
        /// </summary>
        public int BytesSend { get; internal set; }

        /// <summary>
        /// 從資料傳送完成到主機第一次回應時間(亳秒)。
        /// </summary>
        public int TotalSpendTime { get; internal set; }

        /// <summary>
        /// 取得要傳送的位元組總數，-1代表未知狀態。
        /// </summary>
        public long TotalBytesToSend
        {
            get { return Request.LongLength; }
        }

        /// <summary>
        /// 取得接收的位元組數目，-1代表未知狀態。
        /// </summary>
        public int BytesReceived { get; internal set; }

        /// <summary>
        /// 總共接收位元組。
        /// </summary>
        public long TotalBytesToReceive
        {
            get
            {
                if (Response != null)
                    return Response.LongLength;
                else
                    return -1;
            }
        }

        /// <summary>
        /// 主機的最後回應時間。
        /// </summary>
        public DateTime LastResponseTime { get; private set; }

        /// <summary>
        /// 是否在等待主機的第一個回應。
        /// </summary>
        public bool IsWaitFirstResponse { get; internal set; }

        /// <summary>
        /// 取得與此 Trip 相關聯的 Trip，由 HttpManager 管理其值。
        /// </summary>
        public HttpTrip RelatedTrip { get; internal set; }

        /// <summary>
        /// 取得此 Trip 發生的錯誤，Null 代表沒有錯誤。
        /// 由 HttpManager 管理其值。
        /// </summary>
        public Exception Error { get; internal set; }

        private TripStatus _status = TripStatus.Padding;
        /// <summary>
        /// 取得工作目前的執行狀態。
        /// 由 HttpManager 管理其值。
        /// </summary>
        public TripStatus Status
        {
            get { return _status; }
            internal set
            {
                bool raiseEvent = (_status != value);

                _status = value;

                if (raiseEvent)
                    Manager.OnTripStatusChanged(this);
            }
        }

        /// <summary>
        /// 取得執行 Trip 的執行緒。
        /// </summary>
        public Thread RunningThread { get; private set; }

        /// <summary>
        /// 取得或設定是否要取消工作。
        /// </summary>
        internal bool CancellationRequested { get; set; }

        /// <summary>
        /// 重設最後回應時間。
        /// </summary>
        internal void ResetResponseTime()
        {
            LastResponseTime = DateTime.Now;
        }

        /// <summary>
        /// 取消 Http 要求。
        /// </summary>
        public void Cancel()
        {
            CancellationRequested = true;
        }

        /// <summary>
        /// 發出 ProgressChanged 事件。
        /// </summary>
        internal void RaiseProgressChanged()
        {
            Manager.OnTripProgressChanged(this);

            if (CancellationRequested)
                throw new TripCancelledException(this, CancelType.Trip, "HTTP 要求已被取消(透過 ProgressChanged 事件)。");
        }

        internal HttpTrip(string targetUrl, byte[] request, HttpManager manager)
        {
            RunningThread = Thread.CurrentThread;
            Manager = manager;
            TargetUrl = targetUrl;
            Request = request;
            Response = new byte[0];
            CancellationRequested = false;
            BytesSend = 0;
            BytesReceived = 0;
            //TotalBytesToSend = 0; 這行不需要。
            //TotalBytesToReceive = 0;
            RelatedTrip = null;
            Error = null;
            Status = TripStatus.Padding;
            TotalSpendTime = 0;
            BeginTimestamp = DateTime.Now;
            LastResponseTime = DateTime.Now;
            IsWaitFirstResponse = false;
        }

        /// <summary>
        /// 取得此 Trip 的開始時間。
        /// </summary>
        public DateTime BeginTimestamp { get; private set; }
    }
}
