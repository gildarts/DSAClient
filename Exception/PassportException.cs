/*
 * Create Date：2011/4/13
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 用來表示 DSA 的 Passport 錯誤。
    /// </summary>
    public class PassportException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public PassportException()
            : base()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public PassportException(string msg)
            : base(msg)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="innerException"></param>
        public PassportException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="passportProvider"></param>
        /// <param name="providerContract"></param>
        /// <param name="innerException"></param>
        public PassportException(string msg, string passportProvider, string providerContract, Exception innerException)
            : base(msg, innerException)
        {
            PassportProvider = passportProvider;
            ProviderContract = providerContract;
        }

        /// <summary>
        /// 
        /// </summary>
        public string PassportProvider { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string ProviderContract { get; private set; }
    }
}
