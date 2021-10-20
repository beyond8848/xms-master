using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Xms.File
{
    public static class XmlSerializeHelper
    {
        /// <summary>
        /// 将实体对象转换成XML
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="obj">实体对象</param>
        public static string XmlSerialize<T>(T obj)
        {
            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    Type t = obj.GetType();
                    XmlSerializer serializer = new XmlSerializer(obj.GetType());
                    serializer.Serialize(sw, obj);
                    sw.Close();
                    return sw.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("将实体对象转换成XML异常", ex);
            }
        }

        /// <summary>
        /// 将XML转换成实体对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="strXML">XML</param>
        public static T DESerializer<T>(string strXML) where T : class
        {
            try
            {
                using (StringReader sr = new StringReader(strXML))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    return serializer.Deserialize(sr) as T;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("将XML转换成实体对象异常", ex);
            }
        }

        public static void AddPointer(string xmlpath, string nodeName, Dictionary<string, string> dic)
        {
            if (System.IO.File.Exists(xmlpath))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    StreamReader reader = new StreamReader(xmlpath);
                    doc.Load(reader);
                    reader.Close();
                    reader.Dispose();
                    XmlNode node = doc.ChildNodes[1].SelectSingleNode(nodeName);
                    foreach (KeyValuePair<string, string> pair in dic)
                    {
                        XmlElement element = doc.CreateElement(pair.Key);
                        element.InnerText = pair.Value;
                        node.AppendChild(element);
                    }
                    //if (System.IO.File.Exists(xmlpath))
                    //    System.IO.File.Delete(xmlpath);
                    doc.Save(xmlpath);
                }
                catch (Exception ex)
                {
                    throw new Exception("XML添加节点异常", ex);
                }
            }
            else
            {
                throw new Exception("不存在指定的文件");
            }
        }
    }
}
