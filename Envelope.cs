/*
 * Create Date：2010/10/22
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
    /// 代表 DSA 的封包。
    /// </summary>
    public class Envelope : IXmlable
    {
        internal const string EnvelopeElement = "Envelope";
        internal const string HeaderElement = "Header";
        internal const string BodyElement = "Body";

        internal const string TargetContractElement = "TargetContract";
        internal const string TargetServiceElement = "TargetService";
        internal const string SecurityTokenElement = "SecurityToken";

        internal const string StatusElement = "Status"; // Header/Status
        internal const string StatusCodeElement = "Code"; // Header/Status/Code
        internal const string StatusMessageElement = "Message"; // Header/Status/Message

        //internal const string DSFaultElement = "DSFault"; //Header/DSFault
        //internal const string FaultElement = "Fault"; //Header/DSFault/Fault
        //internal const string FaultSourceElement = "Source"; //Header/DSFault/Fault
        //internal const string FaultCodeElement = "Code"; //Header/DSFault/Fault
        //internal const string FaultMessageElement = "Message"; //Header/DSFault/Fault
        //internal const string FaultDetailElement = "Detail"; //Header/DSFault/Fault

        /// <summary>
        /// 
        /// </summary>
        public Envelope()
        {
            Headers = new HeaderCollection();
            Body = new XmlStringHolder("<Content/>");
            TransportBySecureTunnel = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        public Envelope(IXmlable body)
            : this()
        {
            Body = body;
        }

        /// <summary>
        /// 取得 DSA 標頭資料。
        /// </summary>
        public HeaderCollection Headers { get; protected set; }

        /// <summary>
        /// 取得或設定 DSA Body 的資料。
        /// </summary>
        public IXmlable Body { get; set; }

        /// <summary>
        /// 取得或設定是否使用安全通道傳送資料。
        /// </summary>
        public bool? TransportBySecureTunnel { get; set; }

        /// <summary>
        /// 解析 Xml 字串，將狀態寫入到指定的物件中。
        /// </summary>
        /// <param name="outerXml"></param>
        /// <returns></returns>
        public static Envelope Parse(string outerXml)
        {
            Envelope env = new Envelope();
            using (StringReader sr = new StringReader(outerXml))
            {
                using (XmlReader xr = XmlReader.Create(sr, XmlParsing.ReaderSettings))
                {
                    ReadToEnvelope(xr);
                    ReadToHeader(xr);

                    ParseHeader(env.Headers, xr);

                    ReadToBody(xr);

                    env.Body = new XmlStringHolder(xr.ReadInnerXml());
                }
            }
            return env;
        }

        /// <summary>
        /// 將 Reader 移動到 Envelope 元素，如果找不到則丟出例外。
        /// </summary>
        /// <param name="reader"></param>
        internal static void ReadToEnvelope(XmlReader reader)
        {
            if (!reader.ReadToFollowing(Envelope.EnvelopeElement))
                throw new EnvelopeSpecificationException(string.Format("缺少「{0}」元素。", Envelope.EnvelopeElement));
        }

        /// <summary>
        /// 將 Reader 移動到 Header 元素，如果找不到則丟出例外。
        /// </summary>
        /// <param name="reader"></param>
        internal static void ReadToHeader(XmlReader reader)
        {
            if (!reader.ReadToFollowing(Envelope.HeaderElement))
                throw new EnvelopeSpecificationException(string.Format("缺少「{0}」元素。", Envelope.EnvelopeElement));
        }

        /// <summary>
        /// 解析 Header。
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="xr"></param>
        internal static void ParseHeader(HeaderCollection headers, XmlReader xr)
        {
            XmlElement xmlheaders = XmlHelper.ParseAsDOM(xr.ReadOuterXml());
            foreach (XmlNode each in xmlheaders.ChildNodes)
            {
                if (each.NodeType != XmlNodeType.Element) continue;

                headers[each.LocalName] = each.OuterXml;
            }
        }

        /// <summary>
        /// 將 Reader 移動到 Body 元素，如果找不到則丟出例外。
        /// </summary>
        /// <param name="reader"></param>
        internal static void ReadToBody(XmlReader reader)
        {
            if (reader.LocalName == Envelope.BodyElement) return;

            if (!reader.ReadToFollowing(Envelope.BodyElement))
                throw new EnvelopeSpecificationException(string.Format("缺少「{0}」元素。", Envelope.BodyElement));
        }

        #region IXmlObject 成員

        /// <summary>
        /// 
        /// </summary>
        public string XmlString
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                XmlWriterSettings setting = new XmlWriterSettings();
                setting.OmitXmlDeclaration = true;
                using (XmlWriter writer = XmlWriter.Create(builder, setting))
                {
                    writer.WriteStartElement("Envelope");
                    {
                        writer.WriteStartElement("Header");
                        {
                            writer.WriteRaw(Headers.XmlString);
                        }
                        writer.WriteEndElement();
                        writer.WriteStartElement("Body");
                        {
                            if (Body != null)
                                writer.WriteRaw(Body.XmlString);
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        public void WriteTo(string fileName)
        {
            XmlHelper.WriteTo(this, fileName);
        }

        #endregion
    }
}
