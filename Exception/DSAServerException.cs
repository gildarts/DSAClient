using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 用來表示 DSA Server 發生錯誤時。
    /// </summary>
    public class DSAServerException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public DSAServerException()
            : base()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public DSAServerException(string msg)
            : base(msg)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="innerException"></param>
        public DSAServerException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        /// <param name="msg"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="innerException"></param>
        public DSAServerException(string status, string msg, string request, string response, Exception innerException)
            : this(msg, innerException)
        {
            Status = status;
            Request = request;
            Response = response;
        }

        /// <summary>
        /// 
        /// </summary>
        public string AccessPoint { get; set; }

        /// <summary>
        /// DSA Server  的狀態代碼。
        /// </summary>
        public string Status { get; private set; }

        /// <summary>
        /// Request 資料。
        /// </summary>
        public string Request { get; private set; }

        /// <summary>
        /// Response  資料
        /// </summary>
        public string Response { get; private set; }
    }
}
