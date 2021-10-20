using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Xms.Logging.AppLog;
using Xms.Sdk.Client;
using Xms.Web.Framework.Context;
using Xms.Web.Framework.Controller;
using Xms.Web.Models;

namespace Xms.Web.Controllers
{
    [Route("{org}/DataArchive/{action}")]
    public class DataArchiveController : ApiControllerBase
    {
        private readonly IDataFinder _dataFinder;
        private readonly IDataUpdater _dataUpdater;
        private readonly ILogService _logService;

        //private readonly string ASIPFolderPoolDir = System.AppDomain.CurrentDomain.BaseDirectory + "ASIP_POOL_FILES";
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
        public IActionResult DoDataArchive(DataArchiveModel model)
        {
            string jmodel = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            ViewData["model"] = jmodel;
            return View(model);
        }
    }
}
