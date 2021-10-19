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

namespace Xms.Web.Api
{

    [Route("{org}/api/data/DoDataArchive")]
    public class DataArchiveController :  ApiControllerBase
    {
        private readonly IDataFinder _dataFinder;
        private readonly IDataUpdater _dataUpdater;
        private readonly ILogService _logService;

        private readonly string ASIPFolderPoolDir = System.AppDomain.CurrentDomain.BaseDirectory+"ASIP_POOL_FILES";
        public DataArchiveController(IWebAppContext appContext
            , IDataFinder dataFinder
            , IDataUpdater dataUpdater
            , ILogService logService
             )
            : base(appContext)
        {
            _dataFinder = dataFinder;
            _dataUpdater = dataUpdater;
            _logService = logService;
        }

        [Description("预归档")]
        [HttpPost]
        public IActionResult DoDataArchive(string[] reimbursmentIds)
        {
            bool processSussess = false;
            foreach(var id in reimbursmentIds)
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
                            string destFile = ASIPFolderPoolDir + "\\" + claimNo + ".zip";
                            if (System.IO.File.Exists(file))
                            {
                                System.IO.File.Move(file, destFile);
                                Core.Data.Entity updateEntity = new Core.Data.Entity("Reimbursement");
                                updateEntity.SetAttributeValue("IsArchived", 1);
                                updateEntity.SetIdValue(reimbursmentId);
                                _dataUpdater.Update(updateEntity);
                            }
                            processSussess = true;
                        }
                    }
                }catch(Exception ex)
                {
                    _logService.Error(ex);
                }
            }

            return processSussess.UpdateResult(T);
        }
    }
}
