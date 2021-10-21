using Microsoft.AspNetCore.Mvc;
using Xms.Schema.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Xms.Web.Framework.Context;
using Xms.Web.Framework.Controller;
using Xms.Sdk.Client;
using Xms.Core.Data;
using Xms.Web.Framework.Mvc;
using Xms.Logging.AppLog;
using Xms.Web.Models;
using Xms.File;
using System.Xml;
using System.IO.Compression;

namespace Xms.Web.Api
{

    [Route("{org}/api/data/{action}")]
    public class DataArchiveController : ApiControllerBase
    {
        private readonly IDataFinder _dataFinder;
        private readonly IDataUpdater _dataUpdater;
        private readonly ILogService _logService;
        private readonly IMetaFileGenerator _metaFileGenerator;

        private readonly string ASIPFolderPoolDir = System.AppDomain.CurrentDomain.BaseDirectory + "ASIP_POOL_FILES";
        public DataArchiveController(IWebAppContext appContext
            , IDataFinder dataFinder
            , IDataUpdater dataUpdater
            , ILogService logService
            , IMetaFileGenerator metaFileGenerator
             )
            : base(appContext)
        {
            _dataFinder = dataFinder;
            _dataUpdater = dataUpdater;
            _logService = logService;
            _metaFileGenerator = metaFileGenerator;
        }

        [Description("预归档")]
        [HttpPost]
        public IActionResult DoDataArchive(DataArchiveModel model)
        {
            
            //DataArchiveModel model = Newtonsoft.Json.JsonConvert.DeserializeObject<DataArchiveModel>(jmodel);
            bool processSussess = false;
            string[] reimbursmentIds = model.ReimbursmentIds;
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("Storage", model.Storage);
            d.Add("Remark", model.Description);
            foreach (var id in reimbursmentIds)
            {
                try
                {
                    Guid reimbursmentId = new Guid(id);
                    Entity entity = _dataFinder.RetrieveById("Reimbursement", reimbursmentId);
                    if (entity != null)
                    {
                        string claimNo = entity.GetValueOrDefault("ClaimNo").ToString();
                        if (!string.IsNullOrWhiteSpace(claimNo))
                        {
                            string file = ASIPFolderPoolDir + "\\" + claimNo + ".zzip";

                            if (System.IO.File.Exists(file))
                            {
                                string file_temp_path = ASIPFolderPoolDir + "\\" + claimNo;
                                if (!System.IO.Directory.Exists(file_temp_path))
                                    System.IO.Directory.CreateDirectory(file_temp_path);
                                string destFile = ASIPFolderPoolDir + "\\" + claimNo + ".zip";
                                //System.IO.File.Move(file, destFile);
                                //}
                                //解压压缩包
                                ZipArchiveHelper.UnZip(file, file_temp_path);
                                XmlSerializeHelper.AddPointer(file_temp_path + "\\案卷说明.xml", "ArchiveItemInstance", d);
                                _metaFileGenerator.GetRemittanceReceiptAttachFiles(reimbursmentId, file_temp_path);
                                ZipArchiveHelper.CreatZip(_logService, file_temp_path, destFile, CompressionLevel.Fastest);
                                ZipArchiveHelper.DeleteFolder(file_temp_path);
                                Core.Data.Entity updateEntity = new Core.Data.Entity("Reimbursement");
                                updateEntity.SetAttributeValue("IsArchived", 1);
                                updateEntity.SetIdValue(reimbursmentId);
                                _dataUpdater.Update(updateEntity,true);
                            }
                            processSussess = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logService.Error(ex);
                }
            }
            return processSussess.UpdateResult(T);
        }
    }
}