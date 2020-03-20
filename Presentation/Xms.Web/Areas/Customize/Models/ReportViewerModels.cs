using FastReport.Web;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Xms.Web.Areas.Customize.Models
{
    public class ReportViewerModels
    {
        public WebReport webReport;

        public DataSet reportDataSource;

        public string reportTemplate;
    }
}
