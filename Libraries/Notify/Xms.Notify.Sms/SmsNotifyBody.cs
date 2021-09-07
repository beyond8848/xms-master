using System;
using Xms.Notify.Abstractions;

namespace Xms.Notify.Sms
{
    /// <summary>
    /// 短信通知消息体
    /// </summary>
    public class SmsNotifyBody : NotifyBody
    {
        public string To { get; set; }

        public SmsNotifyBody(NotifyBody parentObj)
        {
            SynchronizationProperties(parentObj, this);
        }

        void SynchronizationProperties(object src, object des)
        {
            Type srcType = src.GetType();
            object val;
            foreach (var item in srcType.GetProperties())
            {
                val = item.GetValue(src);
                item.SetValue(des, val);

            }
        }
    }
}