/*
 * Create Date：2010/10/22
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FISCA.DSAClient.HttpUtil
{
    /// <summary>
    /// 用來表示 HttpTrip 失敗，需要重新再試一次的例外狀況。
    /// </summary>
    internal class RetryRequiredException : Exception
    {
        public RetryRequiredException(Exception causeException)
            : base(string.Empty, causeException)
        {
            if (causeException == null)
                throw new ArgumentException("建立 TripRetryRequiredException 時，參數 causeException 不可為 Null。");
        }
    }
}
