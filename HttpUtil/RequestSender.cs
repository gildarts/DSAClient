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
    internal class RequestSender
    {
        public RequestSender(HttpTrip trip, HttpSetup setup)
        {
            Trip = trip;
            Setup = setup;
        }

        public HttpTrip Trip { get; set; }

        public HttpSetup Setup { get; set; }

        public void SendRequest(HttpWebRequest httpreq)
        {
            Trip.RequestHeaders = httpreq.Headers;

            if (Trip.Request == null)
                throw new ArgumentNullException("未提供任何 Request 資料。");

            if (Setup.Method == WebRequestMethods.Http.Post)
            {
                //TotalBytesToSend 一開始就已經決定，這裡不需要處理。
                using (Stream output = httpreq.GetRequestStream())
                {
                    Trip.ResetResponseTime();
                    byte[] data = Trip.Request;

                    int offset = 0, count = Setup.DataBlockSize, len = data.Length;

                    while (offset < len)
                    {
                        if ((offset + count) > len)
                            count = len - offset;

                        output.Write(data, offset, count);
                        Trip.ResetResponseTime();

                        Trip.BytesSend = offset + count;
                        Trip.RaiseProgressChanged();

                        offset += count;
                    }
                }
            }
        }
    }
}
