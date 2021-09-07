using System;
using System.Collections.Generic;
using System.Text;

namespace Xms.File
{
    public interface IMetaFileGenerator
    {
        void GenerateFileXML(Guid entityId);
    }
}
