using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 
    /// </summary>
    public class SecureTunnelException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="ex"></param>
        public SecureTunnelException(string msg, Exception ex)
            : base(msg, ex)
        {
        }
    }
}
