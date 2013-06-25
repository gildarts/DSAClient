using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Net;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 
    /// </summary>
    internal class ErrorReport
    {
        private Dictionary<Type, IExtraProcesser> _collect_types;
        private List<Type> _subclasses = new List<Type>();

        /// <summary>
        /// 
        /// </summary>
        public ErrorReport()
        {
            _collect_types = new Dictionary<Type, IExtraProcesser>();
            AddType(typeof(Exception), true);
        }

        /// <summary>
        /// 將 Exception 物件的相關資訊產生成 Xml 資料。
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string Generate(Exception ex)
        {
            ErrorReport report = new ErrorReport();
            report.AddType(typeof(WebRequest), true);
            report.AddType(typeof(WebResponse), true);
            return report.Transform(ex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        public void AddType(Type type)
        {
            AddType(type, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="includeSubclass"></param>
        public void AddType(Type type, bool includeSubclass)
        {
            if (_collect_types.ContainsKey(type)) return;

            _collect_types.Add(type, null);

            if (includeSubclass)
                _subclasses.Add(type);
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="type"></param>
        ///// <param name="processer"></param>
        //public void AddType(Type type, IExtraProcesser processer)
        //{
        //    if (_collect_types.ContainsKey(type)) return;

        //    _collect_types.Add(type, processer);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="type"></param>
        ///// <param name="processer"></param>
        ///// <param name="includeSubclass"></param>
        //public void AddType(Type type, IExtraProcesser processer, bool includeSubclass)
        //{
        //    if (_collect_types.ContainsKey(type)) return;

        //    _collect_types.Add(type, processer);

        //    if (includeSubclass)
        //        _subclasses.Add(type);
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public string Transform(Exception ex)
        {
            XmlTextWriter writer = new XmlTextWriter(new MemoryStream(), Encoding.UTF8);
            writer.Formatting = Formatting.Indented;

            writer.WriteStartElement(ex.GetType().Name);
            {
                Transform(writer, ex);
            }
            writer.WriteEndElement();

            writer.Flush();
            writer.BaseStream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(writer.BaseStream, Encoding.UTF8);

            string result = reader.ReadToEnd();
            reader.Close();

            return result;
        }

        private void Transform(XmlWriter writer, object ex)
        {
            Type ext = ex.GetType();
            foreach (PropertyInfo each in ext.GetProperties())
            {
                if (!each.CanRead)
                    continue;

                object value = null;

                try
                {
                    value = each.GetValue(ex, null);
                }
                catch (Exception err)
                {
                    writer.WriteStartElement("ReadPropertyError");
                    writer.WriteAttributeString("PropertyName", each.Name);
                    writer.WriteString("讀取屬性錯誤：" + err.Message);
                    writer.WriteEndElement();
                    continue;
                }

                writer.WriteStartElement(each.Name);
                writer.WriteAttributeString("PropertyType", each.PropertyType.Name);
                {
                    if (value != null)
                    {
                        writer.WriteAttributeString("InstanceType", value.GetType().Name);

                        if (_collect_types.ContainsKey(value.GetType()) || Subclasses(value.GetType()))
                            Transform(writer, value);
                        else
                            writer.WriteString(value.ToString());
                    }
                }
                writer.WriteEndElement();
            }

            //writer.WriteComment("自定錯誤訊息處理流程所產生的資料。");
            //writer.WriteStartElement("額外資訊");
            //{
            //    if (_collect_types.ContainsKey(ex.GetType()))
            //    {
            //        IExtraProcesser processer = _collect_types[ex.GetType()];

            //        if (processer != null)
            //        {
            //            ExtraInformation[] infos = processer.Process(ex);

            //            if (infos != null)
            //            {
            //                foreach (ExtraInformation each in infos)
            //                {
            //                    writer.WriteStartElement(each.Name);
            //                    writer.WriteString(each.Data);
            //                    writer.WriteEndElement();
            //                }
            //            }
            //        }
            //    }
            //}
            //writer.WriteEndElement();
        }

        private bool Subclasses(Type type)
        {
            foreach (Type each in _subclasses)
            {
                if (type.IsSubclassOf(each))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal class ExtraInformation
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        public ExtraInformation(string name, string data)
        {
            Name = name;
            Data = data;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string Data { get; private set; }
    }

    /// <summary>
    /// 
    /// </summary>
    internal interface IExtraProcesser
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        ExtraInformation[] Process(object instance);
    }
}
