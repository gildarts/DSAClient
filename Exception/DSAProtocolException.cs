/*
 * Create Date：2010/10/29
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 用來表示 DSA 的 Protocol 錯誤。
    /// </summary>
    public class DSAProtocolException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public DSAProtocolException()
            : base()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public DSAProtocolException(string msg)
            : base(msg)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="innerException"></param>
        public DSAProtocolException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="accessPoint"></param>
        /// <param name="innerException"></param>
        public DSAProtocolException(string msg, string request, string response, string accessPoint, Exception innerException)
            : base(msg, innerException)
        {
            Request = request;
            Response = response;
            TargetAccessPoint = accessPoint;
        }

        /// <summary>
        /// 取得目標 AccessPoint。
        /// </summary>
        public string TargetAccessPoint { get; private set; }

        /// <summary>
        /// 取得 Request 資料。
        /// </summary>
        public string Request { get; private set; }

        /// <summary>
        /// 取得 Response 資料。
        /// </summary>
        public string Response { get; private set; }
    }
}
