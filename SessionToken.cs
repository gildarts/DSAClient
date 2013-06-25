using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 代表 Session 的安全代符。
    /// </summary>
    public class SessionToken : SecurityToken
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="originToken"></param>
        public SessionToken(string sessionId, SecurityToken originToken)
        {
            SessionID = sessionId;
            OriginToken = originToken;
        }

        /// <summary>
        /// 取得 SessionID。
        /// </summary>
        public string SessionID { get; private set; }

        /// <summary>
        /// 取得換得此 Session 的原始 SecurityToken，一般來說 Session 都是用其他的 SecurityToken 換來的。
        /// </summary>
        public SecurityToken OriginToken { get; private set; }

        #region ISecurityToken 成員

        /// <summary>
        /// SecurityToken 類型。
        /// </summary>
        public override string TokenType
        {
            get { return "Session"; }
        }

        #endregion

        #region IXmlable 成員

        /// <summary>
        /// 
        /// </summary>
        protected internal override string XmlString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                using (XmlWriter writer = XmlWriter.Create(sb, XmlParsing.WriterSettings))
                {
                    writer.WriteStartElement("SecurityToken");
                    writer.WriteAttributeString("Type", TokenType);
                    writer.WriteRaw(SessionID);
                    writer.WriteEndElement();
                }
                return sb.ToString();
            }
        }

        #endregion
    }
}
