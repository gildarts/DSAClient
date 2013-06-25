/*
 * Create Date：2010/11/3
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 負責持有 Xml 的資料，此類別不會對 Xml 進行任何解析動作
    /// </summary>
    public class XmlStringHolder : IXmlable
    {
        /// <summary>
        /// 
        /// </summary>
        public XmlStringHolder()
        {
            XmlString = string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outerXml"></param>
        public XmlStringHolder(string outerXml)
            : this()
        {
            XmlString = outerXml;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        public XmlStringHolder(XmlElement element)
            : this()
        {
            XmlString = element.OuterXml;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        public XmlStringHolder(XElement element)
            : this()
        {
            XmlString = element.ToString(SaveOptions.DisableFormatting);
        }

        #region IXmlable 成員

        /// <summary>
        /// 取得或設定 Xml 資料。
        /// </summary>
        public string XmlString { get; set; }

        /// <summary>
        /// 將 Xml 資料寫入到指定的檔案。
        /// </summary>
        /// <param name="fileName"></param>
        public void WriteTo(string fileName)
        {
            XmlHelper.WriteTo(this, fileName);
        }

        #endregion
    }
}
