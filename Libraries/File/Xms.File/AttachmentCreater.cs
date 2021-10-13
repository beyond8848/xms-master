using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xms.Configuration;
using Xms.Context;
using Xms.Core.Data;
using Xms.File.Extensions;
using Xms.Identity;
using Xms.Infrastructure.Utility;
using Xms.OCR;
using Xms.Sdk.Abstractions;
using Xms.Sdk.Abstractions.Query;
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
        private readonly IDataFinder _dataFinder;

        public AttachmentCreater(IAppContext appContext
            , IDataCreater dataCreater
            , IWebHelper webHelper
            , IDataFinder dataFinder
            , ISettingFinder settingFinder)
            : base(appContext)
        {
            _dataCreater = dataCreater;
            _webHelper = webHelper;
            _settingFinder = settingFinder;
            _dataFinder = dataFinder;
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
        /// 验证字符串是否是数字
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private bool IsNum(string str)
        {
            Regex r = new Regex(@"^[+-]?\d*(,\d{3})*(\.\d+)?$");
            if (r.IsMatch(str))
            {
                return true;
            }
            return false;
        }

        public virtual async Task<List<Entity>> CreateManyAsync(Guid entityId, Guid objectId, List<IFormFile> files,Func<string,Invoice> func,Func<string,string> func1)
        {
            //附件
            List<Entity> attachments = new List<Entity>();
            List<Entity> reimbursedDetails = new List<Entity>();
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
                        Invoice invoice = func(savePath);
                        NormalInvoice ni = invoice.Normal;
                        
                        //if(this.VerifyInvoiceNoExisits(ni.InvoiceNo))
                        //{
                        //    string errorInfo = "改发票号码:" + ni.InvoiceNo + "已经存在或者已报销过！";
                        //    func1(errorInfo);
                        //    continue;
                        //}

                        if(ni!=null &&ni.PriceTaxTotal_Num!=null && !IsNum(ni.PriceTaxTotal_Num))
                        {
                            ni.PriceTaxTotal_Num = ni.PriceTaxTotal_Num.Replace("￥", "").Replace(" ", "");
                        }
                        else
                        {
                            //ni = new NormalInvoice
                            //{
                            //    InvoicingDate = System.DateTime.Now.ToString(),
                            //    Title = string.Empty,
                            //    PriceTaxTotal_Num = "0"
                            //};
                        }
                        Entity ent2 = new Entity("ReimbursedDetail")
                       .SetIdValue(id)
                       //地点 not null
                       .SetAttributeValue("Address", "")
                       //数量 not null
                       .SetAttributeValue("Amount", 1)
                       //创建者
                       .SetAttributeValue("CreatedBy", new EntityReference("systemuser", _appContext.GetFeature<ICurrentUser>().SystemUserId))
                       //创建日期
                       .SetAttributeValue("CreatedOn", DateTime.Now)
                       //结束时间 not null
                       .SetAttributeValue("FeeEndTime", Convert.ToDateTime(ni.InvoicingDate))
                       //开始时间 not null
                       .SetAttributeValue("FeeStartTime",Convert.ToDateTime(ni.InvoicingDate))
                       //金额 not null
                       .SetAttributeValue("MoneyAmount", new Money(Convert.ToDecimal(ni.PriceTaxTotal_Num)))
                       //名称 
                       .SetAttributeValue("Name",ni.Title)
                       //组织
                       .SetAttributeValue("OrganizationId", new EntityReference("organization", _appContext.GetFeature<ICurrentUser>().OrganizationId))
                       //所有者
                       .SetAttributeValue("OwnerId", new OwnerObject(OwnerTypes.SystemUser, _appContext.GetFeature<ICurrentUser>().SystemUserId))
                       //所有者类型
                       .SetAttributeValue("OwnerIdType", 1)
                       //所有者部门 not null
                       .SetAttributeValue("OwningBusinessUnit", new EntityReference("businessunit", _appContext.GetFeature<ICurrentUser>().BusinessUnitId))
                       //报销明细 主键 
                       //.SetAttributeValue("ReimbursedDetailId", System.Guid.NewGuid().ToString())
                       //报销类型 not null
                       .SetAttributeValue("ReimbursedType", "1")
                       //报销单 not null
                       .SetAttributeValue("ReimbursementId", new EntityReference("Reimbursement", objectId))
                       //状态
                       .SetAttributeValue("StateCode", "1")
                       //状态描述
                       .SetAttributeValue("StatusCode", "0")
                       //单价 not null
                       .SetAttributeValue("UnitFee", 1.00)
                       .SetAttributeValue("AssociatedFilePath", savePath)
                       .SetAttributeValue("InvoiceCode", ni.InvoiceNo)
                       .SetAttributeValue("InvoiceDM",ni.InvoiceID)
                       .SetAttributeValue("ArchiveNo", "");
                        reimbursedDetails.Add(ent2);
                        ////////////////////////////////////////////////////////////////////////////////////
                    }
                }
            }
            //保存附件
            if (attachments.Count > 0&&reimbursedDetails.Count>0)
            {
                //保存附件
                _dataCreater.CreateMany(attachments);
                //保存报销明细
                _dataCreater.CreateMany(reimbursedDetails);
                return attachments;//.Select(x=>x["cdnpath"].ToString()).ToList();
            }
            return null;
        }

    
        public bool VerifyInvoiceNoExisits(string invoiceNo)
        {
            var query = new QueryExpression("ReimbursedDetail", _appContext.GetFeature<ICurrentUser>().UserSettings.LanguageId);
            query.ColumnSet.AddColumns("ReimbursedDetailId");
            query.Criteria.AddCondition("InvoiceCode", ConditionOperator.Equal, invoiceNo);
            bool isExisits = false;
            if(_dataFinder.Retrieve(query,true)!=null)
            {
                isExisits = true;
            }
            return isExisits;
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
            List<Entity> reimbursedDetails = new List<Entity>();
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
                          Invoice invoice = Util.OCR_Invoice(@"D:\test\fapiao.pdf", @"F:\VS\PaddleOCR-release-2.1\config.txt", false);
                        //NormalInvoice ins = invoice.Normal;
                        //Company cp = invoice.Company();
                        //Project pj = invoice.Project();
                        NormalInvoice ni = new NormalInvoice();
                        Entity ent2 = new Entity("ReimbursedDetail")
                       .SetIdValue(id)
                       //地点 not null
                       .SetAttributeValue("Address", "")
                       //数量 not null
                       .SetAttributeValue("Amount", 1)
                       //创建者
                       .SetAttributeValue("CreatedBy", new EntityReference("systemuser", _appContext.GetFeature<ICurrentUser>().SystemUserId))
                       //创建日期
                       .SetAttributeValue("CreatedOn", DateTime.Now)
                       //结束时间 not null
                       .SetAttributeValue("FeeEndTime", ni.InvoicingDate)
                       //开始时间 not null
                       .SetAttributeValue("FeeStartTime", ni.InvoicingDate)
                       //金额 not null
                       .SetAttributeValue("MoneyAmount", new Money(Convert.ToDecimal(ni.PriceTaxTotal_Num)))
                       //名称 
                       .SetAttributeValue("Name", "电子发票")
                       //组织
                       .SetAttributeValue("OrganizationId", new EntityReference("organization", _appContext.GetFeature<ICurrentUser>().OrganizationId))
                       //所有者
                       .SetAttributeValue("OwnerId", new OwnerObject(OwnerTypes.SystemUser, _appContext.GetFeature<ICurrentUser>().SystemUserId))
                       //所有者类型
                       .SetAttributeValue("OwnerIdType", 1)
                       //所有者部门 not null
                       .SetAttributeValue("OwningBusinessUnit", new EntityReference("businessunit", _appContext.GetFeature<ICurrentUser>().BusinessUnitId))
                       //报销明细 主键 
                       .SetAttributeValue("ReimbursedDetailId", System.Guid.NewGuid().ToString())
                       //报销类型 not null
                       .SetAttributeValue("ReimbursedType", "1")
                       //报销单 not null
                       .SetAttributeValue("ReimbursementId", new EntityReference("Reimbursement", objectId))
                       //状态
                       .SetAttributeValue("StateCode", "1")
                       //状态描述
                       .SetAttributeValue("StatusCode", "0")
                       //单价 not null
                       .SetAttributeValue("UnitFee", 1.00)
                       .SetAttributeValue("AssociatedFilePath", savePath)
                       .SetAttributeValue("InvoiceCode", ni.InvoiceNo)
                       .SetAttributeValue("ArchiveNo","");

                        reimbursedDetails.Add(ent2);
                        ////////////////////////////////////////////////////////////////////////////////////
                    }
                }
            }
            //保存附件
            if (attachments.Count > 0)
            {
                //保存附件
                _dataCreater.CreateMany(attachments);
                //保存报销明细
                _dataCreater.CreateMany(reimbursedDetails);
                return attachments;//.Select(x=>x["cdnpath"].ToString()).ToList();
            }
            return null;
        }
    }
}