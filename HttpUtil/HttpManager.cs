/*
 * Create Date：2010/10/22
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;

namespace FISCA.DSAClient.HttpUtil
{
    /// <summary>
    /// 負責管理 Http 要求、Http 取消、傳送接收進度、失敗重新要求(Retry)、取消重新要求、
    /// Timeout 相關工作。此類別執行緒安全。
    /// 使用此類別的相關「事件」必需要處理多執行緒問題，因為此類別最重要的方法「Send」，可能會
    /// 在不同執行緒同時叫用，而事件引發會在叫用的執行緒上，所以封鎖事件執行，會停止該 Trip 執行。
    /// 此類別預設會由所有 Connection 共用一個實體，但也可個別指定。
    /// </summary>
    [Serializable()]
    public class HttpManager
    {
        /// <summary>
        /// 
        /// </summary>
        public HttpManager()
        {
            MaxRetryCount = 0;
            _current_trips = new Dictionary<HttpTrip, object>();
            HttpSetup = new HttpSetup();
        }

        /// <summary>
        /// 傳送 Request 到指定的 Url，並取得 Response，並在過程中引發相關事件。
        /// </summary>
        /// <param name="url">目標 Url。</param>
        /// <param name="request">Request 資料。</param>
        /// <returns>Response 資料。</returns>
        /// <exception cref="TripCancelledException">當 Client 端取消 Trip 時發生。</exception>
        public byte[] Send(string url, byte[] request)
        {
            return ExecuteTrip(new HttpTrip(url, request, this), MaxRetryCount);
        }

        private byte[] ExecuteTrip(HttpTrip trip, int mrcount)
        {
            byte[] response = new byte[0];

            //執行，並把 MaxRetryCount 減 1，因為要減去本次 Execute。
            int maxretrycount = --mrcount;

            //將 Trip 加入到管理清單中。
            lock (_current_trips) { _current_trips.Add(trip, null); }

            try
            {
                //引發 Trip Starting 事件，在這事件前端可以決定取消此 Trip 執行。
                HttpTripStartingEventArgs startingArg = OnTripStarting(trip);

                //取消 Trip 的執行。
                if (startingArg.Trip.CancellationRequested)
                    throw new TripCancelledException(trip, CancelType.Trip, "HTTP 要求已被取消(透過 TripStarting 事件)。");

                try
                {
                    trip.Status = TripStatus.Running;

                    /*
                     * 執行工作。 Trip 物件會依據 HttpSetup.BufferSize 設定，
                     * 定期透過 TripProgressChanged 事件回報 Progress。
                     * ProgressChanged 事件可進行取消 Trip 動作。
                     * 如果有指定 HttpSetupOverridden 屬性則會履蓋 Manager 預設值。
                     * */
                    HttpSetup setup = startingArg.HttpSetupOverridden == null ? HttpSetup : startingArg.HttpSetupOverridden;
                    new HttpTripExecutor(trip, setup).Execute();
                    response = trip.Response; //獲得 HTTP Response。

                    trip.Status = TripStatus.Success;
                }
                catch (RetryRequiredException ex) //進行 Retry 流程.
                {
                    //當重試次數小於 0 時，不重試直接爆出去。
                    if (maxretrycount < 0)
                        throw ex.InnerException;

                    //設定造成本次錯誤的 Exception。
                    trip.Error = ex.InnerException;

                    //會依據 MaxRetryCount 遞迴進行 Retry。
                    response = RetryTrip(trip, ex.InnerException, maxretrycount);
                }
            }
            catch (TripCancelledException) //當前端取消進行中的 Trip 時會到這裡。
            {
                trip.Status = TripStatus.Cancelled;
                throw;
            }
            catch (Exception ex)    //任何其他的錯誤，都到這裡。
            {
                trip.Error = ex;
                trip.Status = TripStatus.Failure;
                throw;
            }
            finally
            {
                try //這個 try...catch 是為了保證 finally 的 lock 區段一定會被執行。
                {
                    //引發 Trip Completed 事件。
                    OntripCompleted(trip);
                }
                catch { throw; }
                finally
                {
                    //從管理清單中移除 Trip。
                    lock (_current_trips) { _current_trips.Remove(trip); }
                }
            }
            return response;
        }

        private byte[] RetryTrip(HttpTrip failedTrip, Exception failReason, int maxretrycount)
        {
            //引發 RetryOccurring 前將狀態設定為 Padding。
            failedTrip.Status = TripStatus.Padding;

            //RetryOccured 事件可讓 Client 決定是否要取消重試。
            HttpTripRetryEventArgs arg = OnTripRetryOccurring(failedTrip);

            if (arg.Cancel)
                throw new TripCancelledException(failedTrip, CancelType.Retry, "Http 重試被前端取消(透過 TripRetryOccured 事件)。");

            //進入 Retry 狀態。
            failedTrip.Status = TripStatus.Retry;

            //建立新的 Trip 進行重試。
            HttpTrip retrytrip = new HttpTrip(failedTrip.TargetUrl, failedTrip.Request, this);

            //指定造成重試的 Trip。
            retrytrip.RelatedTrip = failedTrip;

            return ExecuteTrip(retrytrip, maxretrycount);
        }

        /// <summary>
        /// 當 HttpTrip 失敗時，最大重試次數，預設值為 0 次。
        /// </summary>
        public int MaxRetryCount { get; set; }

        private Dictionary<HttpTrip, object> _current_trips;
        /// <summary>
        /// 正在執行的 Trip 清單。
        /// </summary>
        public List<HttpTrip> CurrentTrips
        {
            get
            {
                lock (_current_trips) { return new List<HttpTrip>(_current_trips.Keys); }
            }
        }

        /// <summary>
        /// 取得或設定 Http 的相關設定。更改此設定只會影響新的 HttpTrip。
        /// </summary>
        public HttpSetup HttpSetup { get; set; }

        #region Raise Event Methods
        /// <summary>
        /// 當 Trip 要開始之前。
        /// </summary>
        /// <param name="trip"></param>
        protected virtual HttpTripStartingEventArgs OnTripStarting(HttpTrip trip)
        {
            HttpTripStartingEventArgs arg = new HttpTripStartingEventArgs(trip);

            if (TripStarting != null)
                TripStarting(this, arg);

            return arg;
        }

        /// <summary>
        /// 當 Trip 進度變更時。
        /// </summary>
        /// <param name="trip"></param>
        internal protected virtual void OnTripProgressChanged(HttpTrip trip)
        {
            if (TripProgressChanged != null)
                TripProgressChanged(this, new HttpTripEventArgs(trip));
        }

        /// <summary>
        /// 當準備重試之前。
        /// </summary>
        /// <param name="trip"></param>
        /// <returns></returns>
        internal protected virtual HttpTripRetryEventArgs OnTripRetryOccurring(HttpTrip trip)
        {
            HttpTripRetryEventArgs arg = new HttpTripRetryEventArgs(trip);

            if (TripRetryOccurring != null)
                TripRetryOccurring(this, arg);

            return arg;
        }

        /// <summary>
        /// 當 Trip 狀態變更時。
        /// </summary>
        /// <param name="trip"></param>
        internal protected virtual void OnTripStatusChanged(HttpTrip trip)
        {
            if (TripStatusChanged != null)
                TripStatusChanged(this, new HttpTripEventArgs(trip));
        }

        /// <summary>
        /// 當 Trip 完成時。
        /// </summary>
        /// <param name="trip"></param>
        protected virtual void OntripCompleted(HttpTrip trip)
        {
            if (TripCompleted != null)
                TripCompleted(this, new HttpTripEventArgs(trip));
        }
        #endregion

        #region Events
        /// <summary>
        /// 當 Trip 開始之前，此事件可能會由數個不同的執行緒上同時發生。
        /// </summary>
        public event EventHandler<HttpTripStartingEventArgs> TripStarting;

        /// <summary>
        /// 當 Trip 的處理進度變更時，此事件可能會由數個不同的執行緒上同時發生。
        /// </summary>
        public event EventHandler<HttpTripEventArgs> TripProgressChanged;

        /// <summary>
        /// 當 Trip 失敗，重試發生之前，事件參數中的 Trip 代表的是失敗的 Trip，此事件可能會由數個不同的執行緒上同時發生。
        /// </summary>
        public event EventHandler<HttpTripRetryEventArgs> TripRetryOccurring;

        /// <summary>
        /// 當 Trip 狀態變更時，此事件可能會由數個不同的執行緒上同時發生。
        /// </summary>
        public event EventHandler<HttpTripEventArgs> TripStatusChanged;

        /// <summary>
        /// 當 Trip 結束時，不管是否成功，所有 HttpTrip 最後都會引發此事件，此事件可能會由數個不同的執行緒上同時發生。
        /// </summary>
        public event EventHandler<HttpTripEventArgs> TripCompleted;
        #endregion
    }

    #region Event Args

    /// <summary>
    /// HttpTrip 事件參數。
    /// </summary>
    public class HttpTripEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="trip"></param>
        public HttpTripEventArgs(HttpTrip trip)
        {
            Trip = trip;
        }

        /// <summary>
        /// 相關聯的 Trip 物件。
        /// </summary>
        public HttpTrip Trip { get; private set; }
    }

    /// <summary>
    /// HttpTrip 重試事件參數。
    /// </summary>
    public class HttpTripRetryEventArgs : HttpTripEventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="trip"></param>
        public HttpTripRetryEventArgs(HttpTrip trip)
            : base(trip)
        {
            Cancel = false;
        }

        /// <summary>
        /// 取得或設定是否取消 HttpTrip 重試。
        /// </summary>
        public bool Cancel { get; set; }
    }

    /// <summary>
    /// HttpTrip Starting 事件參數。
    /// </summary>
    public class HttpTripStartingEventArgs : HttpTripEventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="trip"></param>
        public HttpTripStartingEventArgs(HttpTrip trip)
            : base(trip)
        {
            HttpSetupOverridden = null;
        }

        /// <summary>
        /// 取得或設定 HTTP 設定，如果未指定，會使用 HttpManager 的預設值執行 HttpTrip。
        /// </summary>
        public HttpSetup HttpSetupOverridden { get; set; }
    }
    #endregion
}
