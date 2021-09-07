using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastReport;
using FastReport.Web;
using Microsoft.AspNetCore.Mvc;
using Xms.Business.DataAnalyse.Report;
using Xms.Sdk.Abstractions.Query;
using Xms.Sdk.Client;
using Xms.Solution;
using Xms.Web.Areas.Customize.Models;
using Xms.Web.Customize.Controllers;
using Xms.Web.Framework.Context;
using Xms.Core.Data;

namespace Xms.Web.Areas.Customize.Controllers
{
    public class ReportViewerController : CustomizeBaseController
    {
        private readonly IReportService _reportService;
        private readonly IDataFinder _dataFinder;
        public ReportViewerController(IWebAppContext appContext,
            IReportService reportService,
            ISolutionService solutionService,IDataFinder dataFinder) : base(appContext, solutionService)
        {
            _reportService= reportService;
            _dataFinder = dataFinder;
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

            Guid guid = Guid.Parse(Request.Query["id"].ToString());
            string entityName = Request.Query["EntityName"].ToString();
            var report = _reportService.FindById(guid);
            string reportBodyText = report.BodyText;
            byte[] byteArray = Encoding.ASCII.GetBytes(reportBodyText);
            MemoryStream stream = new MemoryStream(byteArray);
            reportViewModels.webReport.Report.Load(stream);

            QueryExpression query = new QueryExpression("ArchiveTable");
            query.ColumnSet.AllColumns = true;
            var archiveData = _dataFinder.RetrieveAll(query, true);

            string reportFolder = @"C:\Users\Administrator\Downloads\FastReport-2020.1.0\FastReport-2020.1.0\Demos\Reports\";
            //string reportfile = "Simple List";
            //reportViewModels.webReport.Report.Load(Path.Combine((reportFolder), $"{reportfile}.frx"));
            //reportViewModels.reportDataSource.ReadXml(Path.Combine(reportFolder, "nwind.xml"));
            reportViewModels.webReport.Report.RegisterData(reportViewModels.reportDataSource, "NorthWind");

            return View(reportViewModels);
        }
    }
}