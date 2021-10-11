using System;
using System.Collections.Generic;
using System.Text;
using Xms.Flow.Core.Events;
using Xms.Flow.Domain;

namespace Xms.File
{
    public interface IMetaFileGenerator
    {
        void ZipFiles(Guid mainEntityID, List<WorkFlowInstance> workFlowInstances);
        string GenerateFileXML(Guid entityId, List<WorkFlowProcess> workFlowProcesses,out ArchiveInstructions archiveInstructions);

        void CreateWorkFlowPDF(List<WorkFlowProcess> workFlowProcesses);
    }
}
