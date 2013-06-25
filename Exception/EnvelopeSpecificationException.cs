using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 用來表示 Envelop 規格不正確例外。
    /// </summary>
    public class EnvelopeSpecificationException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public EnvelopeSpecificationException()
            : base()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public EnvelopeSpecificationException(string msg)
            : base(msg)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="innerException"></param>
        public EnvelopeSpecificationException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="innerException"></param>
        /// <param name="envelope"></param>
        public EnvelopeSpecificationException(string msg,Exception innerException, string envelope)
            : base(msg)
        {
            Envelope = envelope;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Envelope { get; private set; }
    }
}
