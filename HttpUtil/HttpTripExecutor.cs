/*
 * Create Date：2010/10/26
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace FISCA.DSAClient.HttpUtil
{
    internal class HttpTripExecutor
    {
        private HttpTrip Trip { get; set; }

        private HttpSetup Setup { get; set; }

        private bool ForceDisableCompression { get; set; }

        public HttpTripExecutor(HttpTrip trip, HttpSetup setup)
        {
            Trip = trip;
            Setup = setup;
            ForceDisableCompression = false;
        }

        public void Execute()
        {
            //建立Http連線，設定基本參數。
            HttpWebRequest objreq = (HttpWebRequest)WebRequest.Create(Trip.TargetUrl);
            objreq.KeepAlive = Setup.KeepAlive;
            objreq.Timeout = Setup.Timeout;
            objreq.ReadWriteTimeout = 10000;
            objreq.Method = Setup.Method;
            objreq.ContentType = Setup.ContentType;
            objreq.Proxy = Setup.WebProxy;
            //objreq.SendChunked = true;  //會影響效能。
            //objreq.AllowWriteStreamBuffering = false;  //會影響效能。

            //是否允許 Response 壓縮編碼。
            if (Setup.AllowCompression && !ForceDisableCompression)
                objreq.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            try
            {
                Trip.ResetResponseTime();
                int t1 = Environment.TickCount;
                //傳送 Request 到 Server。
                new RequestSender(Trip, Setup).SendRequest(objreq);

                Trip.IsWaitFirstResponse = true;
                HttpWebResponse objrsp = objreq.GetResponse() as HttpWebResponse;
                Trip.IsWaitFirstResponse = false;
                Trip.ResetResponseTime();

                //從 Server 取得 Response。
                new ResponseReceiver(Trip, Setup).ReceiveResponse(objrsp);
                Trip.TotalSpendTime = (Environment.TickCount - t1);
            }
            catch (WebException ex)
            {
                try
                {
                    if (ex.Response != null)
                        Trip.ResponseHeaders = ex.Response.Headers;

                    using (Stream rsp = ex.Response.GetResponseStream())
                    {
                        using (MemoryStream buffering = new MemoryStream())
                        {
                            int bufferSize = Setup.DataBlockSize;
                            byte[] data = new byte[bufferSize];
                            int count = -1;

                            while ((count = rsp.Read(data, 0, bufferSize)) > 0)
                                buffering.Write(data, 0, count);

                            Trip.Response = buffering.GetBuffer();
                        }
                    }
                }
                catch { }

                //代表前端取消了這次的 HTTP Request。
                if (ex.Status == WebExceptionStatus.RequestCanceled && Trip.CancellationRequested)
                    throw new TripCancelledException(Trip, CancelType.Trip, "HTTP Request 已取消。");

                throw new RetryRequiredException(ex);
            }
            catch (InvalidDataException ex)
            {
                if (ForceDisableCompression) //如果已經是強制閉關壓縮還爆掉，就直接爆出去!!
                    throw;

                Console.WriteLine(ErrorReport.Generate(ex));
                ForceDisableCompression = true;
                Execute();
            }
        }
    }
}
