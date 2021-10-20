using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Xms.Web.Models
{
    public class DataArchiveModel
    {
        public string[] ReimbursmentIds { get; set; }

        public string Storage { get; set; }

        public string Description { get; set; }

        public string CallBack { get; set; } = "function(){}";
    }
}
