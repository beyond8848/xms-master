using System;
using Xms.Context;
using Xms.Core.Context;
using Xms.Core.Data;
using Xms.Identity;
using Xms.Sdk.Abstractions.Query;
using Xms.Sdk.Client;

namespace Xms.File
{
    /// <summary>
    /// 附件查找服务
    /// </summary>
    public class AttachmentFinder : AttachmentService, IAttachmentFinder
    {
        private readonly IAppContext _appContext;
        private readonly IDataFinder _dataFinder;

        public AttachmentFinder(IAppContext appContext
            , IDataFinder dataFinder) : base(appContext)
        {
            _appContext = appContext;
            _dataFinder = dataFinder;
        }

        public virtual Entity FindById(Guid id)
        {
            return _dataFinder.RetrieveById(EntityName, id);
        }

        public virtual Entity FindReimbursmentAttachById(Guid id)
        {
            return _dataFinder.RetrieveById("ReimbursmentDetailAttach", id);
        }

        public virtual PagedList<Entity> QueryPagedFromReimbursementDetailAttach(int page, int pageSize, Guid reimbursmentDetailId)
        {
            var query = new QueryExpression("ReimbursmentDetailAttach", _appContext.GetFeature<ICurrentUser>().UserSettings.LanguageId);
            query.PageInfo = new PagingInfo() { PageNumber = page, PageSize = pageSize };
            query.ColumnSet.AddColumns("ReimbursmentDetailAttachId", "AttachmentId", "EntityId", "ObjectId", "name", "cdnpath", "description", "createdon", "createdby", "ownerid", "owningbusinessunit");
            query.Criteria.AddCondition("ReimbursmentDetailID", ConditionOperator.Equal, reimbursmentDetailId);
            query.AddOrder("createdon", OrderType.Descending);
            return _dataFinder.RetrieveMultiple(query);
        }

        public virtual PagedList<Entity> QueryPaged(int page, int pageSize, Guid entityId, Guid objectId)
        {
            var query = new QueryExpression(EntityName, _appContext.GetFeature<ICurrentUser>().UserSettings.LanguageId);
            query.PageInfo = new PagingInfo() { PageNumber = page, PageSize = pageSize };
            query.ColumnSet.AddColumns("attachmentid", "name", "cdnpath", "description", "createdon", "createdby", "ownerid", "owningbusinessunit");
            query.Criteria.AddCondition("entityid", ConditionOperator.Equal, entityId);
            query.Criteria.AddCondition("objectid", ConditionOperator.Equal, objectId);
            query.AddOrder("createdon", OrderType.Descending);
            return _dataFinder.RetrieveMultiple(query);
        }
    }
}