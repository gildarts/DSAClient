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
using System.IO.Compression;

namespace FISCA.DSAClient.HttpUtil
{
    internal class ResponseReceiver
    {
        public ResponseReceiver(HttpTrip trip, HttpSetup setup)
        {
            Trip = trip;
            Setup = setup;
        }

        public HttpTrip Trip { get; set; }

        public HttpSetup Setup { get; set; }

        public void ReceiveResponse(HttpWebResponse httprsp)
        {
            int bufferSize = Setup.DataBlockSize;
            byte[] data = new byte[bufferSize];

            Trip.ResponseHeaders = httprsp.Headers; //記錄 Response Header。
            //bool decompress = string.Equals(httprsp.GetResponseHeader("Content-Encoding"), "gzip", StringComparison.OrdinalIgnoreCase);
            //Trip.TotalBytesToReceive = httprsp.ContentLength;

            using (Stream rspstream = httprsp.GetResponseStream(), buffering = new MemoryStream())
            {
                Trip.ResetResponseTime();

                //using (Stream readsource = (decompress ? new GZipStream(rspstream, CompressionMode.Decompress) : rspstream))
                using (Stream readsource = rspstream)
                {
                    int count = -1;

                    Trip.BytesReceived = 0;
                    while ((count = readsource.Read(data, 0, bufferSize)) > 0)
                    {
                        Trip.ResetResponseTime();
                        buffering.Write(data, 0, count);
                        Trip.BytesReceived += count;
                        Trip.RaiseProgressChanged();
                    }

                    data = new byte[buffering.Length];
                    buffering.Seek(0, SeekOrigin.Begin);
                    buffering.Read(data, 0, (int)buffering.Length);
                }
            }

            Trip.Response = data;
        }
    }
}
