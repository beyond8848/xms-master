using System;
using System.Collections.Generic;
using Xms.Event.Abstractions;
using Xms.File;
using Xms.Flow;
using Xms.Flow.Abstractions;
using Xms.Flow.Core.Events;
using Xms.Sdk.Abstractions;
using Xms.Sdk.Abstractions.Query;
using Xms.Sdk.Client;

namespace Xms.EventConsumers.Entity
{
    /// <summary>
    /// 更新业务对象的处理状态
    /// </summary>
    public class UpdateEntityProcessState : IConsumer<WorkFlowExecutedEvent>, IConsumer<WorkFlowCancelledEvent>, IConsumer<WorkFlowStartedEvent>
    {
        private readonly IDataUpdater _dataUpdater;
        private readonly IDataFinder _dataFinder;
        private readonly IWorkFlowProcessFinder _workFlowProcessFinder;
        private readonly IWorkFlowInstanceService _workFlowInstanceService;
        private readonly IMetaFileGenerator _metaFileGenerator;

        public UpdateEntityProcessState(IDataUpdater dataUpdater,
            IDataFinder dataFinder, 
            IWorkFlowProcessFinder workFlowProcessFinder, 
            IWorkFlowInstanceService workFlowInstanceService,
            IMetaFileGenerator metaFileGenerator)
        {
            _dataUpdater = dataUpdater;
            _dataFinder = dataFinder;
            _workFlowProcessFinder = workFlowProcessFinder;
            _workFlowInstanceService = workFlowInstanceService;
            _metaFileGenerator = metaFileGenerator;
        }
        
        public void HandleEvent(WorkFlowExecutedEvent eventMessage)
        {
            //修改bug 如果流程有多个节点，多分支情况，不是下个环节处理就结束流程,
            WorkFlowProcessState workFlowProcessState = eventMessage.Result.NextHandlerId != null && eventMessage.Result.NextHandlerId.Count > 0 ? WorkFlowProcessState.Processing : eventMessage.Context.ProcessState;
            this.Update(eventMessage.Context.EntityMetaData, eventMessage.Context.InstanceInfo.ObjectId, workFlowProcessState);
           
            if(workFlowProcessState == WorkFlowProcessState.Passed)
            {
                GenerateArchivePage(eventMessage.Context.EntityMetaData, eventMessage.Context.InstanceInfo.ObjectId, eventMessage, workFlowProcessState);
            }
        }

        public void HandleEvent(WorkFlowCancelledEvent eventMessage)
        {
            this.Update(eventMessage.EntityMetaData, eventMessage.ObjectId, WorkFlowProcessState.Canceled);
        }

        public void HandleEvent(WorkFlowStartedEvent eventMessage)
        {
            this.Update(eventMessage.Context.EntityMetaData, eventMessage.Context.ObjectId, WorkFlowProcessState.Processing);
        }

        private void GenerateArchivePage(Schema.Domain.Entity entityMetaData, Guid objectId, WorkFlowExecutedEvent eventMessage, WorkFlowProcessState state)
        {
            //如果流程通过，则进行生成数据包操作。
            if (state == WorkFlowProcessState.Passed)
            {
                var instances = _workFlowInstanceService.Query(n => n
                .Where(f => f.EntityId == entityMetaData.EntityId && f.ObjectId == objectId)
                .Sort(s => s.SortDescending(f => f.CreatedOn))
                );

                foreach(var instance in instances)
                {
                    var steps = _workFlowProcessFinder.Query(n => n
                   .Where(f => f.WorkFlowInstanceId == eventMessage.Context.InstanceInfo.WorkFlowInstanceId && f.StateCode != WorkFlowProcessState.Disabled)
                   .Sort(s => s.SortAscending(f => f.StepOrder)).Sort(s => s.SortAscending(f => f.StateCode)));
                    instance.Steps = steps;
                }

                _metaFileGenerator.ZipFiles(eventMessage.Context.InstanceInfo.ObjectId, instances);
            }
        }

        /// <summary>
        /// 更新业务对象的处理状态
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="objectId"></param>
        /// <param name="state"></param>
        /// <param name="entityName"></param>
        private void Update(Schema.Domain.Entity entityMetaData, Guid objectId, WorkFlowProcessState state)
        {
            var entity = new Core.Data.Entity(entityMetaData.Name);
            entity.SetIdValue(objectId);
            entity.SetAttributeValue("ProcessState", new OptionSetValue((int)state));
            _dataUpdater.Update(entity, true);
        }
    }
}