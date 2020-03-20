using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FastReport.Web;
using Microsoft.AspNetCore.Mvc;
using Xms.Business.DataAnalyse.Report;
using Xms.Solution;
using Xms.Web.Areas.Customize.Models;
using Xms.Web.Customize.Controllers;
using Xms.Web.Framework.Context;

namespace Xms.Web.Areas.Customize.Controllers
{
    public class ReportViewerController : CustomizeBaseController
    {
        private readonly IReportService _reportService;
        public ReportViewerController(IWebAppContext appContext,
            IReportService reportService,
            ISolutionService solutionService) : base(appContext, solutionService)
        {
            _reportService= reportService;
        }


        /// <summary>
        /// Show  web report in browser.
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var reportViewModels = new ReportViewerModels
            {
                webReport = new WebReport(),
                reportDataSource = new DataSet(),
                reportTemplate = string.Empty 
            };

            string reportFolder = @"C:\Users\Administrator\Downloads\FastReport-2020.1.0\FastReport-2020.1.0\Demos\Reports\";
            string reportfile = "Simple List";
            reportViewModels.webReport.Report.Load(Path.Combine((reportFolder), $"{reportfile}.frx"));
            
            reportViewModels.reportDataSource.ReadXml(Path.Combine(reportFolder, "nwind.xml"));
            reportViewModels.webReport.Report.RegisterData(reportViewModels.reportDataSource, "NorthWind");

            return View(reportViewModels);
        }
    }
}