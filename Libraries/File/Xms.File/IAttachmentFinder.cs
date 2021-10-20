using System;
using Xms.Core.Context;
using Xms.Core.Data;

namespace Xms.File
{
    public interface IAttachmentFinder
    {
        Entity FindById(Guid id);

        Entity FindReimbursmentAttachById(Guid id);

        PagedList<Entity> QueryPaged(int page, int pageSize, Guid entityId, Guid objectId);

        PagedList<Entity> QueryPagedFromReimbursementDetailAttach(int page, int pageSize, Guid reimbursmentDetailId);
    }
}