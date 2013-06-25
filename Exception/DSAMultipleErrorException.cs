/*
 * Create Date：2010/11/2
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 用來表示多個 DSA 失敗。
    /// </summary>
    public class DSAMultipleErrorException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="errors"></param>
        public DSAMultipleErrorException(string msg, IList<Exception> errors)
            : base(msg, null)
        {
            Errors = errors;
        }

        /// <summary>
        /// 包含所有解析錯誤的例外集合。
        /// </summary>
        public IList<Exception> Errors { get; private set; }
    }
}