/*
 * Create Date：2010/10/26
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace FISCA.DSAClient.HttpUtil
{
    /// <summary>
    /// 代表 Http 要求的相關設定。
    /// </summary>
    [Serializable()]
    public class HttpSetup
    {
        /// <summary>
        /// 
        /// </summary>
        public HttpSetup()
        {
            DataBlockSize = 1024 * 100; //預設 100KB。
            KeepAlive = false;
            Timeout = 100000; //100 秒。
            Method = WebRequestMethods.Http.Post;
            ContentType = "text/xml";
            WebProxy = new WebProxy();
            AllowCompression = true;
        }

        /// <summary>
        /// 每次傳送/接收資料的大小，以 Byte 為單位設定，預設為 100 KB (1024 * 100)，注意：此數值會影響 ProgressChanged 事件頻率，
        /// 如果設定的較小，將會提升事件引發頻率。
        /// </summary>
        public int DataBlockSize { get; set; }

        /// <summary>
        /// 取得或設定是否啟用 Http 的 KeepAlive 機制，預設值為不啟用。啟用此機制會增加效能，但是連線數多時
        /// 可能會造成 Server 負擔，另外啟用的狀況下有如果有防火牆在中間問題會比較多。
        /// </summary>
        public bool KeepAlive { get; set; }

        /// <summary>
        /// 取得或設定 Http 的要求方法，僅支援「POST、GET」兩種，預設為「POST」。
        /// 此屬性會被每個新建立的 HttpTrip 使用，如果 HttpTrip已經在執行
        /// 才修改此屬性，該 HttpTrip不受影響。
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// 取得或設定 HTTP  Content-Type 標頭的值。預設為值「text/xml」(image/file)。
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// 取得或設定 HTTP 要求的 Proxy 資訊。預設值為不使用任何 Proxy。
        /// </summary>
        public IWebProxy WebProxy { get; set; }

        /// <summary>
        /// 取得或設定 Timeout 時間，以亳秒為單位，預設為 100 秒(100,000 亳秒)。
        /// 此屬性會被每個新建立的 HttpTrip 使用，如果 HttpTrip 已經在執行
        /// 才修改此屬性，該 HttpTrip不受影響。
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// 取得或設定 HTTP Response 是否可接受壓縮編碼，預設值是 True。
        /// 如果允許 Response 壓縮編碼，會加速網路傳輸效率。此屬性設定為「True」時會
        /// 傳送「AcceptEncoding:gzip」的 HTTP 標頭，但 Server 會不會回傳壓縮編碼資料則看 Server 設定，
        /// 一但 Response 是壓縮編碼，則進度處理無法取得確切的 Response 大小。
        /// </summary>
        public bool AllowCompression { get; set; }
    }
}
