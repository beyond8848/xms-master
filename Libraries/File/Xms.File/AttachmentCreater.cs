﻿using Microsoft.AspNetCore.Http;
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
using Xms.Logging.AppLog;
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
        private readonly ILogService _logService;

        public AttachmentCreater(IAppContext appContext
            , IDataCreater dataCreater
            , IWebHelper webHelper
            , IDataFinder dataFinder
            , ISettingFinder settingFinder
            ,ILogService logService)
            : base(appContext)
        {
            _dataCreater = dataCreater;
            _webHelper = webHelper;
            _settingFinder = settingFinder;
            _dataFinder = dataFinder;
            _logService = logService;
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


        /// <summary>
        /// 附件银行转汇单
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="objectId"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        public virtual async Task<List<Entity>> DoCashRemittance(Guid entityId, Guid objectId, List<IFormFile> files)
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
                        Entity ent = new Entity("ReimbursmentDetailAttach")
                        .SetIdValue(id)
                         .SetAttributeValue("name", file.FileName)
                        .SetAttributeValue("filesize", file.Length)
                        .SetAttributeValue("mimetype", file.ContentType)
                        .SetAttributeValue("cdnpath", Path + fileName)
                        .SetAttributeValue("entityid", entityId)
                        .SetAttributeValue("objectid", objectId)
                        .SetAttributeValue("attachtype", "银行转汇单");
                        attachments.Add(ent);
                    }
                }
            }
            //保存附件
            if (attachments.Count > 0)
            {
                //保存附件
                _dataCreater.CreateMany(attachments);
                return attachments;//.Select(x=>x["cdnpath"].ToString()).ToList();
            }
            return null;
        }

        public virtual async Task<List<Entity>> DoInvoiceAttach(Guid entityId, Guid objectId, List<IFormFile> files)
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
                        Entity ent = new Entity("ReimbursmentDetailAttach")
                        .SetIdValue(id)
                        .SetAttributeValue("name", file.FileName)
                        .SetAttributeValue("filesize", file.Length)
                        .SetAttributeValue("mimetype", file.ContentType)
                        .SetAttributeValue("cdnpath", Path + fileName)
                        .SetAttributeValue("ReimbursmentDetailID", objectId)
                        .SetAttributeValue("AttachmentId", id)
                        .SetAttributeValue("ObjectId", objectId)
                        .SetAttributeValue("EntityId",new Guid("0322DA55-9E6E-47D7-A55A-72A6AB1B1873"));
                        attachments.Add(ent);
                    }
                }
            }
            //保存附件
            if (attachments.Count > 0)
            {
                //保存附件
                _dataCreater.CreateMany(attachments);
               
                return attachments;//.Select(x=>x["cdnpath"].ToString()).ToList();
            }
            return null;
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
                        .SetAttributeValue("objectid", objectId)
                        .SetAttributeValue("attachtype", "电子发票");
                        
                        attachments.Add(ent);

                        if (fileName.Substring(fileName.Length - 4) == ".pdf"
                            || fileName.Substring(fileName.Length - 4) == ".jpg"
                            || fileName.Substring(fileName.Length - 4) == ".png"
                            || fileName.Substring(fileName.Length - 5) == ".jpeg")
                        {
                            //这里加上OCR识别逻辑////////////////////////////////////////////////////////////////.
                            //识别完之后，加入到发票明细表中，同时调用ajax刷新明细表页面。
                            Invoice invoice = func(savePath);
                            NormalInvoice @in = invoice.Normal;
                            if (@in != null && @in.PriceTaxTotal_Num != null && !IsNum(@in.PriceTaxTotal_Num))
                            {
                                @in.PriceTaxTotal_Num = @in.PriceTaxTotal_Num.Replace("￥", "").Replace(" ", "");
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
                            string serviceName;
                            if (@in.Items.Count != 0)
                                serviceName = @in.Items[0].Trade;
                            else
                                serviceName = "";
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
                            .SetAttributeValue("FeeEndTime", Convert.ToDateTime(@in.InvoicingDate))
                            //开始时间 not null
                            .SetAttributeValue("FeeStartTime", Convert.ToDateTime(@in.InvoicingDate))
                            .SetAttributeValue("InvoiceDate", Convert.ToDateTime(@in.InvoicingDate))
                            //金额 not null
                            .SetAttributeValue("MoneyAmount", new Money(Convert.ToDecimal(@in.PriceTaxTotal_Num)))
                            //名称 
                            .SetAttributeValue("Name", serviceName)
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
                            //.SetAttributeValue("UnitFee", "")
                            .SetAttributeValue("AssociatedFilePath", savePath)
                            .SetAttributeValue("InvoiceCode", @in.InvoiceNo)
                            .SetAttributeValue("InvoiceDM", @in.InvoiceID)
                            .SetAttributeValue("ArchiveNo", "")
                            .SetAttributeValue("ServiceName",serviceName)
                            .SetAttributeValue("Seller", @in.Seller.Name)
                            .SetAttributeValue("Buyer", @in.Buyer.Name);
                            reimbursedDetails.Add(ent2);
                        }
                        //if(this.VerifyInvoiceNoExisits(ni.InvoiceNo))
                        //{
                        //    string errorInfo = "改发票号码:" + ni.InvoiceNo + "已经存在或者已报销过！";
                        //    func1(errorInfo);
                        //    continue;
                        //}
                        else if (fileName.Substring(fileName.Length - 4) == ".ofd")
                        {
                            try
                            {

                                OfdSharp.Reader.OfdReader reader = new OfdSharp.Reader.OfdReader(savePath);
                                OfdSharp.Core.Invoice.InvoiceInfo @in = reader.GetInvoiceInfo();

                                string serviceName;
                                if (@in.GoodsInfos.Count != 0)
                                    serviceName = @in.GoodsInfos[0].Item;
                                else
                                    serviceName = "";

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
                            .SetAttributeValue("FeeEndTime", Convert.ToDateTime(@in.IssueDate))
                            //开始时间 not null
                            .SetAttributeValue("FeeStartTime", Convert.ToDateTime(@in.IssueDate))
                            .SetAttributeValue("InvoiceDate", Convert.ToDateTime(@in.IssueDate))
                            //金额 not null
                            .SetAttributeValue("MoneyAmount", new Money(Convert.ToDecimal(@in.TaxInclusiveTotalAmount)))
                            //名称 
                            .SetAttributeValue("Name", serviceName)
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
                            //单价
                            //.SetAttributeValue("UnitFee", "")
                            .SetAttributeValue("AssociatedFilePath", savePath)
                            .SetAttributeValue("InvoiceCode", @in.InvoiceNo)
                            .SetAttributeValue("InvoiceDM", @in.InvoiceCode)
                            .SetAttributeValue("ServiceName",serviceName)
                            .SetAttributeValue("ArchiveNo", "")
                            .SetAttributeValue("Seller", @in.Seller.SellerName)
                            .SetAttributeValue("Buyer", @in.Buyer.BuyerName);
                                reimbursedDetails.Add(ent2);
                            }
                            catch (Exception ex)
                            {
                                _logService.Error(ex);
                            }
                        }

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