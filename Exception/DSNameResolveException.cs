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
    /// 用來表示解析 DSNS 名稱失敗。
    /// </summary>
    public class DSNameResolveException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public DSNameResolveException()
            : base()
        {
            InitPropertys();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public DSNameResolveException(string msg)
            : base(msg)
        {
            InitPropertys();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="innerException"></param>
        public DSNameResolveException(string msg, Exception innerException)
            : base(msg, innerException)
        {
            InitPropertys();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="nameToResolve"></param>
        /// <param name="nameServer"></param>
        /// <param name="innerException"></param>
        public DSNameResolveException(string msg, string nameToResolve, string nameServer, Exception innerException)
            : base(msg, innerException)
        {
            InitPropertys();
            NameServer = nameServer;
            NameToResolve = nameToResolve;
        }

        private void InitPropertys()
        {
            NameServer = string.Empty;
            NameToResolve = string.Empty;
        }

        /// <summary>
        /// 負責解析的主機位置。
        /// </summary>
        public string NameServer { get; private set; }

        /// <summary>
        /// 解析錯誤的名稱。
        /// </summary>
        public string NameToResolve { get; private set; }
    }
}