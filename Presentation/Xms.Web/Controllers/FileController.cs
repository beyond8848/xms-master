using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Xms.File;
using Xms.Infrastructure.Utility;
using Xms.Logging.AppLog;
using Xms.OCR;
using Xms.Schema.Attribute;
using Xms.Schema.Entity;
using Xms.Sdk.Extensions;
using Xms.Web.Framework.Context;
using Xms.Web.Framework.Controller;
using Xms.Web.Framework.Infrastructure;
using Xms.Web.Framework.Mvc;
using Xms.Web.Models;

namespace Xms.Web.Controllers
{
    /// <summary>
    /// 文件控制器
    /// </summary>
    [Route("{org}/file/[action]")]
    public class FileController : WebControllerBase
    {
        private readonly IEntityFinder _entityFinder;
        private readonly IAttributeFinder _attributeFinder;
        private readonly IAttachmentFinder _attachmentFinder;
        private readonly IWebHelper _webHelper;
        private readonly ILogService _logService;
    

        public FileController(IWebAppContext appContext
            , IEntityFinder entityFinder
            , IAttributeFinder attributeFinder
            , IAttachmentFinder attachmentFinder
            , IWebHelper webHelper
            , ILogService logService)
            : base(appContext)
        {
            _entityFinder = entityFinder;
            _attributeFinder = attributeFinder;
            _attachmentFinder = attachmentFinder;
            _webHelper = webHelper;
            _logService = logService;
        }

        //[Description("文件列表")]
        //public IActionResult Index(AttachmentsModel model)
        //{
        //    return View();
        //}

        [Description("附件列表")]
        public IActionResult AttachmentsDialog(AttachmentsModel model, DialogModel dm)
        {
            if (!Arguments.HasValue(model.EntityId, model.ObjectId))
            {
                return JError(T["parameter_error"]);
            }
            model.EntityMetaData = _entityFinder.FindByName("attachment");
            model.AttributeMetaDatas = _attributeFinder.FindByEntityId(model.EntityMetaData.EntityId);
            var result = _attachmentFinder.QueryPaged(model.Page, model.PageSize, model.EntityId, model.ObjectId);
            model.Items = result.Items;
            model.TotalItems = result.TotalItems;
            ViewData["DialogModel"] = dm;
            return View(model);
        }

        [Description("下载附件")]
        public IActionResult Download(Guid id, string sid, bool preview = false)
        {
            if (!sid.IsCaseInsensitiveEqual(CurrentUser.SessionId))
            {
                return NotFound();
            }
            var result = _attachmentFinder.FindById(id);
            if (result.IsEmpty())
            {
                return NotFound();
            }
            var filePath = result.GetStringValue("cdnpath");
            if (preview)
            {
                if (filePath.IsNotEmpty())
                {
                    return new FileStreamResult(System.IO.File.OpenRead(_webHelper.MapPath(filePath)), result.GetStringValue("mimetype"));
                }
            }
            if (filePath.IsNotEmpty())
            {
                return PhysicalFile(_webHelper.MapPath(filePath), result.GetStringValue("mimetype"), result.GetStringValue("name"));
            }
            else if (result.GetStringValue("body").IsNotEmpty())
            {
                byte[] bt = Convert.FromBase64String(result.GetStringValue("body"));
                //System.IO.MemoryStream stream = new System.IO.MemoryStream(bt, 0, bt.Length);
                return File(bt, result.GetStringValue("mimetype"), result.GetStringValue("name").UrlEncode());
            }
            return NotFound();
        }
    }

    [Route("{org}/file/[action]")]
    public class FileCreaterController : WebControllerBase
    {
        private readonly IAttachmentCreater _attachmentCreater;
        private readonly ILogService _logService;

        public FileCreaterController(IWebAppContext appContext
            , IAttachmentCreater attachmentCreater
            , ILogService logService)
            : base(appContext)
        {
            _attachmentCreater = attachmentCreater;
            _logService = logService;
        }

        [Description("新建附件")]
        public IActionResult Create()
        {
            CreateAttachmentModel model = new CreateAttachmentModel();
            return View(model);
        }

        [Description("新建附件")]
        //[ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(CreateAttachmentModel model)
        {
            if (model.EntityId.Equals(Guid.Empty) || model.ObjectId.Equals(Guid.Empty))
            {
                return JError(T["parameter_error"]);
            }
            string errorInfo = string.Empty;
            if (model.Attachments.NotEmpty())
            {
                Func<string,Invoice> func = (s) =>
                {
                    return DoOCR(s);
                };

                Func<string, string> errorLogs = (s) =>
                {
                    errorInfo = s;
                    return errorInfo;
                };

                var result = await _attachmentCreater.CreateManyAsync(model.EntityId, model.ObjectId, model.Attachments, func, errorLogs).ConfigureAwait(false);

                if (result.NotEmpty()&& string.IsNullOrWhiteSpace(errorInfo))
                {
                    return JOk(T["saved_success"], result);
                }
            }
            return SaveFailure(errorInfo, null);
        }

        private static string _OCRConfigPath;
        public static string OCRConfigPath
        {
            get
            {
                if(string.IsNullOrWhiteSpace(_OCRConfigPath))
                {
                    var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", true, reloadOnChange: true);
                    var config = builder.Build();
                    _OCRConfigPath = config["OCRConfigPath:ConfigPath"];
                }
                return _OCRConfigPath;
            }
        }

        private Invoice DoOCR(string filePath)
        {
            //_logService.Information(OCRConfigPath);
            string paddleConfigFile = OCRConfigPath;
            //_logService.Information(filePath);
            DotNetCoreTest.Invoice invoiceOCR = DotNetCoreTest.StartOCR.Do(filePath, paddleConfigFile);
            Invoice invoice = new Invoice();
            invoice.Normal = new NormalInvoice();
            this.ConvertDotNetCoreTestObjectToXmlOCRObject(invoice.Normal, invoiceOCR.Normal);
            return invoice;
        }

        /// <summary>
        /// 做OCR识别，获取电子发票相关信息
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private List<Invoice> DoOCR(List<IFormFile> list)
        {
            List<Invoice> invoices = new List<Invoice>();
            string paddleConfigFile = OCRConfigPath;
            foreach(var file in list)
            {
               DotNetCoreTest.Invoice invoiceOCR = DotNetCoreTest.StartOCR.Do(file.FileName, paddleConfigFile);
               Invoice invoice = new Invoice();
               this.ConvertDotNetCoreTestObjectToXmlOCRObject(invoice.Normal, invoiceOCR.Normal);
               invoices.Add(invoice);
            }
            return invoices;
        }

        public void ConvertDotNetCoreTestObjectToXmlOCRObject(NormalInvoice normalInvoice, DotNetCoreTest.NormalInvoice normalInvoice1)
        {
            if (normalInvoice1 == null) return;
            normalInvoice.Checker = normalInvoice1.Checker;
            normalInvoice.CheckID = normalInvoice1.CheckID;
            normalInvoice.InvoiceID = normalInvoice1.InvoiceID;
            normalInvoice.InvoiceNo = normalInvoice1.InvoiceNo;
            normalInvoice.Invoicer = normalInvoice1.Invoicer;
            normalInvoice.InvoicingDate = normalInvoice1.InvoicingDate;
            normalInvoice.MachineID = normalInvoice1.MachineID;
            normalInvoice.PriceTaxTotal_CHS = normalInvoice1.PriceTaxTotal_CHS;
            normalInvoice.PriceTaxTotal_Num = normalInvoice1.PriceTaxTotal_Num;
            normalInvoice.Recipient = normalInvoice1.Recipient;
            //normalInvoice.Seller = normalInvoice1.Seller;
            normalInvoice.Title = normalInvoice1.Title;
        }
    }
    

    [Route("{org}/file/[action]")]
    public class FileDeleterController : WebControllerBase
    {
        private readonly IAttachmentDeleter _attachmentDeleter;

        public FileDeleterController(IWebAppContext appContext
            , IAttachmentDeleter attachmentDeleter)
            : base(appContext)
        {
            _attachmentDeleter = attachmentDeleter;
        }

        [Description("删除附件")]
        public IActionResult Delete([FromBody]DeleteAttachmentModel model)
        {
            if (model.EntityId.Equals(Guid.Empty) || model.ObjectId.Equals(Guid.Empty))
            {
                return JError(T["parameter_error"]);
            }
            var flag = _attachmentDeleter.DeleteById(model.EntityId, model.ObjectId,model.RecordId);
            return flag.DeleteResult(T);
        }
    }
}