using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace FISCA.DSAClient
{
    internal class XmlParsing
    {
        public static XmlReaderSettings ReaderSettings
        {
            get
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                return settings;
            }
        }

        public static XmlWriterSettings WriterSettings
        {
            get
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.OmitXmlDeclaration = true;
                settings.Encoding = Encoding.UTF8;
                return settings;
            }
        }
    }
}
