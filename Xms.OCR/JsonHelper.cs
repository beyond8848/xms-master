using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Xms.OCR
{
    public static class JsonHelper
    {
        /// <summary>
        /// 将对象序列化为JSON格式
        /// </summary>
        /// <param name="o">对象</param>
        /// <returns>json字符串</returns>
        public static string SerializeObject(object o)
        {
            string json = JsonConvert.SerializeObject(o);
            return json;
        }

        /// <summary>
        /// 解析JSON字符串生成对象实体
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="json">json字符串(eg.{"ID":"112","Name":"石子儿"})</param>
        /// <returns>对象实体</returns>
        public static T DeserializeJsonToObject<T>(string json) where T : class
        {
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                StringReader sr = new StringReader(json);
                object o = serializer.Deserialize(new JsonTextReader(sr), typeof(T));
                T t = o as T;
                return t;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 解析JSON数组生成对象实体集合
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="json">json数组字符串(eg.[{"ID":"112","Name":"石子儿"}])</param>
        /// <returns>对象实体集合</returns>
        public static List<T> DeserializeJsonToList<T>(string json) where T : class
        {
            JsonSerializer serializer = new JsonSerializer();
            StringReader sr = new StringReader(json);
            object o = serializer.Deserialize(new JsonTextReader(sr), typeof(List<T>));
            List<T> list = o as List<T>;
            return list;
        }

        /// <summary>
        /// 反序列化JSON到给定的匿名对象.
        /// </summary>
        /// <typeparam name="T">匿名对象类型</typeparam>
        /// <param name="json">json字符串</param>
        /// <param name="anonymousTypeObject">匿名对象</param>
        /// <returns>匿名对象</returns>
        public static T DeserializeAnonymousType<T>(string json, T anonymousTypeObject)
        {
            T t = JsonConvert.DeserializeAnonymousType(json, anonymousTypeObject);
            return t;
        }

        /// <summary>
        /// 设置是否格式化json
        /// </summary>
        /// <param name="format"></param>
        public static void SetFormat(bool format)
        {
            if (format)
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    //TypeNameHandling = TypeNameHandling.Objects,
                    //ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
            else
            {
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                {
                    Formatting = Formatting.None
                };
            }
        }
        /// <summary>
        /// json 转换为动态类型
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static dynamic ToDynamic(this string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<dynamic>(json);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

    }
}
