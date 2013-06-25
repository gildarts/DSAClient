/*
 * Create Date：2010/11/2
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 代表 DSA 的標頭集合。
    /// </summary>
    public class HeaderCollection : IXmlable, IEnumerable<string>
    {
        private Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public HeaderCollection()
        {
            Headers = new Dictionary<string, string>();
        }

        /// <summary>
        /// 新增標頭資料。
        /// </summary>
        /// <param name="outerXml">完整的標頭資料。</param>
        public void Add(string outerXml)
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(outerXml), XmlParsing.ReaderSettings))
            {
                if (!XmlHelper.ReadToElement(reader))
                    throw new ArgumentException("無法解析標頭資料。");

                string elmname = reader.LocalName;
                string outerxml = reader.ReadOuterXml();

                if (Contains(elmname))
                    throw new ArgumentException("指定的標頭已存在：" + elmname);
                else
                    Headers.Add(elmname, outerxml);
            }
        }

        /// <summary>
        /// 設定標頭資料，如果該標頭已存在，則會覆蓋。
        /// </summary>
        /// <param name="outerXml"></param>
        public void Set(string outerXml)
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(outerXml), XmlParsing.ReaderSettings))
            {
                if (!XmlHelper.ReadToElement(reader))
                    throw new ArgumentException("無法解析標頭資料。");

                string elmname = reader.LocalName;
                string outerxml = reader.ReadOuterXml();

                if (Contains(elmname))
                    Remove(elmname);

                Add(outerxml);
            }
        }

        /// <summary>
        /// 新增標頭資料。
        /// </summary>
        /// <param name="elmName">標頭名稱。</param>
        /// <param name="innerXml">標頭內容，可以是單純的字串資料。</param>
        public void Add(string elmName, string innerXml)
        {
            using (MemoryStream rawdata = new MemoryStream()) //搞那麼複雜是因為 Parse....
            {
                using (XmlWriter writer = XmlWriter.Create(rawdata, XmlParsing.WriterSettings))
                {
                    writer.WriteStartElement(elmName);
                    writer.WriteRaw(innerXml);
                    writer.WriteEndElement();
                    writer.Flush();

                    rawdata.Seek(0, SeekOrigin.Begin);
                    using (XmlReader reader = XmlReader.Create(rawdata, XmlParsing.ReaderSettings))
                    {
                        //往下讀取到 NodeType 是 Element 為止。
                        if (!XmlHelper.ReadToElement(reader))
                            throw new ArgumentException("無法解析標頭資料。");

                        if (Contains(elmName))
                            throw new ArgumentException("指定的標頭已存在：" + elmName);
                        else
                            Headers.Add(elmName, reader.ReadOuterXml());
                    }
                }
            }
        }

        /// <summary>
        /// 設定標頭資料，如果該標頭已存在，則會覆蓋。
        /// </summary>
        /// <param name="elmName"></param>
        /// <param name="innerXml"></param>
        public void Set(string elmName, string innerXml)
        {
            if (Contains(elmName))
                Remove(elmName);

            Add(elmName, innerXml);
        }

        /// <summary>
        /// 新增標頭資料。
        /// </summary>
        /// <param name="header"></param>
        public void Add(IXmlable header)
        {
            Add(header.XmlString);
        }

        /// <summary>
        ///  設定標頭資料，如果該標頭已存在，則會覆蓋。
        /// </summary>
        /// <param name="header"></param>
        public void Set(IXmlable header)
        {
            Set(header.XmlString);
        }

        /// <summary>
        /// 移除指定名稱的標頭。
        /// </summary>
        /// <param name="elmName"></param>
        public void Remove(string elmName)
        {
            Headers.Remove(elmName);
        }

        /// <summary>
        /// 判斷是否包含指定名稱的標頭。
        /// </summary>
        /// <param name="elmName"></param>
        /// <returns></returns>
        public bool Contains(string elmName)
        {
            return Headers.ContainsKey(elmName);
        }

        /// <summary>
        /// 取得或設定標頭完整內容。
        /// </summary>
        /// <param name="elmName"></param>
        /// <returns></returns>
        public string this[string elmName]
        {
            get { return Headers[elmName]; }
            set { Headers[elmName] = value; }
        }

        /// <summary>
        /// 取得標頭數量。
        /// </summary>
        public int Count { get { return Headers.Count; } }

        #region IXmlable 成員

        /// <summary>
        /// 取得以 Xml 構結表示的物件狀態。
        /// </summary>
        public string XmlString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (string header in Headers.Values)
                    sb.Append(header);
                return sb.ToString();
            }
        }

        /// <summary>
        /// 將物件狀態以 Xml 資料儲存到指定檔案。
        /// </summary>
        /// <param name="fileName"></param>
        public void WriteTo(string fileName)
        {
            using (StreamWriter writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                foreach (string header in Headers.Values)
                    writer.WriteLine(XmlHelper.Format(header));
            }
        }

        #endregion

        #region IEnumerable<string> 成員

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<string> GetEnumerator()
        {
            return Headers.Keys.GetEnumerator();
        }

        #endregion

        #region IEnumerable 成員

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Headers.Keys.GetEnumerator();
        }

        #endregion
    }
}
