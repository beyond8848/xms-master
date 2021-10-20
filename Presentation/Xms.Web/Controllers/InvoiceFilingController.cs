using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel;
using System.Linq;
using Xms.Authorization.Abstractions;
using Xms.Flow;
using Xms.Flow.Abstractions;
using Xms.Flow.Domain;
using Xms.Infrastructure.Utility;
using Xms.QueryView;
using Xms.RibbonButton;
using Xms.Schema.Entity;
using Xms.Schema.RelationShip;
using Xms.Sdk.Abstractions.Query;
using Xms.Sdk.Client;
using Xms.Web.Framework.Context;
using Xms.Web.Framework.Controller;
using Xms.Web.Models;

namespace Xms.Web.Controllers
{
    [Route("{org}/api/Index")]
    public class InvoiceFilingController : AuthenticatedControllerBase
    {
        private readonly IQueryViewFinder _queryViewFinder;
        private readonly IRibbonButtonFinder _ribbonbuttonFinder;
        private readonly IGridService _gridService;
        private readonly IFetchDataService _fetchDataService;
        private readonly IRoleObjectAccessService _roleObjectAccessService;
        private readonly IAggregateService _aggregateService;
        private readonly IRelationShipFinder _relationShipFinder;

        public InvoiceFilingController(IWebAppContext appContext
            , IQueryViewFinder queryViewFinder
            , IRibbonButtonFinder ribbonbuttonFinder
            , IGridService gridService
            , IRoleObjectAccessService roleObjectAccessService
            , IFetchDataService fetchDataService
            , IAggregateService aggregateService
            , IRelationShipFinder relationShipFinder)
            : base(appContext)
        {
            _gridService = gridService;
            _fetchDataService = fetchDataService;
            _queryViewFinder = queryViewFinder;
            _ribbonbuttonFinder = ribbonbuttonFinder;
            _roleObjectAccessService = roleObjectAccessService;
            _aggregateService = aggregateService;
            _relationShipFinder = relationShipFinder;
        }

        public IActionResult Index(DataListModel model)
        {
                  if (HttpContext.Request.Query.ContainsKey("IsArchived"))
            {
                ViewData["DoArchive"] = "True";
            }
            if (!model.EntityId.HasValue || model.EntityId.Equals(Guid.Empty))
            {
                if (!model.QueryViewId.HasValue || model.QueryViewId.Equals(Guid.Empty))
                {
                    if (model.EntityName.IsEmpty())
                    {
                        return NotFound();
                    }
                }
            }

            return View($"~/Views/InvoiceFiling/Index.cshtml", model);
            //return View();
        }
    }
}
