using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 
    /// </summary>
    public class PassportToken : SecurityToken
    {
        private string _token_content;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenContent"></param>
        public PassportToken(IXmlable tokenContent)
        {
            _token_content = tokenContent.XmlString;
            OriginToken = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public PassportToken(IXmlable tokenContent, SecurityToken originToken)
        {
            _token_content = tokenContent.XmlString;
            OriginToken = originToken;
        }

        /// <summary>
        /// 
        /// </summary>
        public override string TokenType
        {
            get { return "SecurityToken"; }
        }

        /// <summary>
        /// 
        /// </summary>
        protected internal override string XmlString
        {
            get
            {
                return "<SecurityToken Type='Passport'>" + _token_content + "</SecurityToken>";
            }
        }

        /// <summary>
        /// Passport 內容。
        /// </summary>
        public string PassportContent
        {
            get { return XmlString; }
        }

        /// <summary>
        /// 取得換得此 PassportToken 的原始 SecurityToken。
        /// </summary>
        public SecurityToken OriginToken { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static PassportToken GetPassport(string accessPoint, string contract, SecurityToken securityToken)
        {
            try
            {
                string srv = "DS.Base.GetPassportToken";
                Envelope rsp = Connection.SendRequest(accessPoint, contract, srv, new Envelope(), securityToken, true);

                return new PassportToken(rsp.Body, securityToken);
            }
            catch (Exception ex)
            {
                throw new PassportException("取得 Passport 錯誤，" + ex.Message, accessPoint, contract, ex);
            }
        }
    }
}
