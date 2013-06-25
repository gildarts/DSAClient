/*
 * Create Date：2010/10/25
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FISCA.DSAClient.HttpUtil
{
    /// <summary>
    /// 取消的動作類型。
    /// </summary>
    public enum CancelType
    {
        /// <summary>
        /// 取消工作本身，例如 HttpTrip 進行之前或進行到一半，使用者取消了。
        /// </summary>
        Trip,
        /// <summary>
        /// 取消重試。
        /// </summary>
        Retry
    }

    /// <summary>
    /// 表示用來傳達 HttpTrip 被取消的例外狀況。
    /// </summary>
    public class TripCancelledException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="trip"></param>
        /// <param name="type"></param>
        /// <param name="message"></param>
        public TripCancelledException(HttpTrip trip, CancelType type, string message)
            : base(message)
        {
            Trip = trip;
            Type = type;
        }

        /// <summary>
        /// 相關聯的 Trip 物件。
        /// </summary>
        public HttpTrip Trip { get; private set; }

        /// <summary>
        /// 取消的動作類型。
        /// </summary>
        public CancelType Type { get; private set; }
    }
}
