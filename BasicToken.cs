using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 代表基本認證的安全代符。
    /// </summary>
    public class BasicToken : SecurityToken
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        public BasicToken(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        /// <summary>
        /// 取得使用者名稱。
        /// </summary>
        public string UserName { get; private set; }

        private string Password { get; set; }

        #region ISecurityToken 成員

        /// <summary>
        /// SecurityToken 類型。
        /// </summary>
        public override string TokenType
        {
            get { return "Basic"; }
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
                    writer.WriteElementString("UserName", UserName);
                    writer.WriteElementString("Password", Password);
                    writer.WriteEndElement();
                }
                return sb.ToString();
            }
        }

        #endregion
    }

}
