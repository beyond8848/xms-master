using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xms.Configuration;
using Xms.Context;
using Xms.Core.Data;
using Xms.File.Extensions;
using Xms.Infrastructure.Utility;
using Xms.OCR;
using Xms.Sdk.Client;

namespace Xms.File
{
    /// <summary>
    /// 附件创建服务
    /// </summary>
    public class AttachmentCreater : AttachmentService, IAttachmentCreater
    {
        private readonly IDataCreater _dataCreater;
        private readonly IWebHelper _webHelper;
        private readonly ISettingFinder _settingFinder;

        public AttachmentCreater(IAppContext appContext
            , IDataCreater dataCreater
            , IWebHelper webHelper
            , ISettingFinder settingFinder)
            : base(appContext)
        {
            _dataCreater = dataCreater;
            _webHelper = webHelper;
            _settingFinder = settingFinder;
        }

        /// <summary>
        /// 创建记录
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="objectId"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public virtual async Task<Entity> CreateAsync(Guid entityId, Guid objectId, IFormFile file)
        {
            if (file.Length > 0)
            {
                string dir = _webHelper.MapPath(Path, true);
                Guid id = Guid.NewGuid();
                string fileName = id.ToString() + System.IO.Path.GetExtension(file.FileName);
                string savePath = dir + fileName;
                await file.SaveAs(savePath, _settingFinder, _webHelper).ConfigureAwait(false);
                Entity ent = new Entity(EntityName)
                .SetIdValue(id)
                .SetAttributeValue("name", file.FileName)
                .SetAttributeValue("filesize", file.Length)
                .SetAttributeValue("mimetype", file.ContentType)
                .SetAttributeValue("cdnpath", Path + fileName)
                .SetAttributeValue("entityid", entityId)
                .SetAttributeValue("objectid", objectId);
                _dataCreater.Create(ent);
                return ent;
            }
            return null;
        }

        /// <summary>
        /// 创建多条记录
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="objectId"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        public virtual async Task<List<Entity>> CreateManyAsync(Guid entityId, Guid objectId, List<IFormFile> files)
        {
            //附件
            List<Entity> attachments = new List<Entity>();
            if (files.NotEmpty())
            {
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    if (file.Length > 0)
                    {
                        string dir = _webHelper.MapPath(Path, true);
                        Guid id = Guid.NewGuid();
                        string fileName = id.ToString() + System.IO.Path.GetExtension(file.FileName);
                        string savePath = dir + fileName;
                        await file.SaveAs(savePath, _settingFinder, _webHelper).ConfigureAwait(false);
                        Entity ent = new Entity(EntityName)
                        .SetIdValue(id)
                        .SetAttributeValue("name", file.FileName)
                        .SetAttributeValue("filesize", file.Length)
                        .SetAttributeValue("mimetype", file.ContentType)
                        .SetAttributeValue("cdnpath", Path + fileName)
                        .SetAttributeValue("entityid", entityId)
                        .SetAttributeValue("objectid", objectId);
                        attachments.Add(ent);

                        //这里加上OCR识别逻辑////////////////////////////////////////////////////////////////.
                        //识别完之后，加入到发票明细表中，同时调用ajax刷新明细表页面。
                        //  Invoice invoice = Util.OCR_Invoice(@"D:\test\fapiao.pdf", @"F:\VS\PaddleOCR-release-2.1\config.txt", false);
                        //NormalInvoice ins = invoice.Normal;
                        //Company cp = invoice.Company();
                        //Project pj = invoice.Project();
                        NormalInvoice ni = new NormalInvoice();
                        Company cp = new Company();
                        Project pj = new Project();
                        Entity ent2 = new Entity("ReimbursedDetail")
                       .SetIdValue(id)
                       //地点 not null
                       .SetAttributeValue("Address","")
                       //数量 not null
                       .SetAttributeValue("Amount","")
                       //创建者
                       .SetAttributeValue("CreatedBy", file.ContentType)
                       //创建日期
                       .SetAttributeValue("CreatedOn", DateTime.Now)
                       //结束时间 not null
                       .SetAttributeValue("FeeEndTime", DateTime.Now)
                       //开始时间 not null
                       .SetAttributeValue("FeeStartTime", DateTime.Now)
                       //金额 not null
                       .SetAttributeValue("MoneyAmount","")
                       //名称 
                       .SetAttributeValue("Name", "")
                       //组织
                       .SetAttributeValue("OrganizationId", "")
                       //所有者
                       .SetAttributeValue("OwnerId", "")
                       //所有者类型
                       .SetAttributeValue("OwnerIdType", "")
                       //所有者部门 not null
                       .SetAttributeValue("OwningBusinessUnit", "")
                       //报销明细 主键 
                       .SetAttributeValue("ReimbursedDetailId", "")
                       //报销类型 not null
                       .SetAttributeValue("ReimbursedType", "")
                       //报销单 not null
                       .SetAttributeValue("ReimbursementId", "")
                       //状态
                       .SetAttributeValue("StateCode", "")
                       //状态描述
                       .SetAttributeValue("StatusCode", "")
                       //单价 not null
                       .SetAttributeValue("UnitFee", "")
                       //时间戳
                       .SetAttributeValue("VersionNumber", "");
                        _dataCreater.Create(ent2);
                        ////////////////////////////////////////////////////////////////////////////////////
                    }
                }
            }
            //保存附件
            if (attachments.Count > 0)
            {
                _dataCreater.CreateMany(attachments);
                return attachments;//.Select(x=>x["cdnpath"].ToString()).ToList();
            }
            return null;
        }
    }
}