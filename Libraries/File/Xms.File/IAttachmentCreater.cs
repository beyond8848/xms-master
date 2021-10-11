using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xms.Core.Data;
using Xms.OCR;

namespace Xms.File
{
    public interface IAttachmentCreater
    {
        Task<Entity> CreateAsync(Guid entityId, Guid objectId, IFormFile file);

        Task<List<Entity>> CreateManyAsync(Guid entityId, Guid objectId, List<IFormFile> files);

        Task<List<Entity>> CreateManyAsync(Guid entityId, Guid objectId, List<IFormFile> files, Func<string,Invoice> func, Func<string, string> func1);
    }
}