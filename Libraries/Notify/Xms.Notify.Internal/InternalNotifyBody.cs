using System;
using Xms.Notify.Abstractions;

namespace Xms.Notify.Internal
{
    /// <summary>
    /// 内部信息提示体
    /// </summary>
    public class InternalNotifyBody : NotifyBody
    {

        public InternalNotifyBody() { }

        public InternalNotifyBody(NotifyBody parentObj)
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
        public Guid ToUserId { get; set; }
        public string LinkTo { get; set; }
        public int TypeCode { get; set; }
    }
}