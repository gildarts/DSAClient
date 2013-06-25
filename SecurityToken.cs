/*
 * Create Date：2010/10/22
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 封裝 DSA 安全代符的基礎類別。
    /// </summary>
    public abstract class SecurityToken
    {
        /// <summary>
        /// SecurityToken 類型。
        /// </summary>
        public abstract string TokenType { get; }

        #region IXmlable 成員

        /// <summary>
        /// 是供 SecurityToken 的完整內容。
        /// </summary>
        protected internal abstract string XmlString { get; }

        #endregion
    }
}
