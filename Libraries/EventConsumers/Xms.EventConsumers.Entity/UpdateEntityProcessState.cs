using System;
using Xms.Event.Abstractions;
using Xms.Flow.Abstractions;
using Xms.Flow.Core.Events;
using Xms.Sdk.Abstractions;
using Xms.Sdk.Client;

namespace Xms.EventConsumers.Entity
{
    /// <summary>
    /// 更新业务对象的处理状态
    /// </summary>
    public class UpdateEntityProcessState : IConsumer<WorkFlowExecutedEvent>, IConsumer<WorkFlowCancelledEvent>, IConsumer<WorkFlowStartedEvent>
    {
        private readonly IDataUpdater _dataUpdater;

        public UpdateEntityProcessState(IDataUpdater dataUpdater)
        {
            _dataUpdater = dataUpdater;
        }

        
        public void HandleEvent(WorkFlowExecutedEvent eventMessage)
        {
            //修改bug 如果流程有多个节点，多分支情况，不是下个环节处理就结束流程,
            WorkFlowProcessState workFlowProcessState = eventMessage.Result.NextHandlerId != null && eventMessage.Result.NextHandlerId.Count > 0 ? WorkFlowProcessState.Processing : eventMessage.Context.ProcessState;
            this.Update(eventMessage.Context.EntityMetaData, eventMessage.Context.InstanceInfo.ObjectId, workFlowProcessState);
        }

        public void HandleEvent(WorkFlowCancelledEvent eventMessage)
        {
            this.Update(eventMessage.EntityMetaData, eventMessage.ObjectId, WorkFlowProcessState.Canceled);
        }

        public void HandleEvent(WorkFlowStartedEvent eventMessage)
        {
            this.Update(eventMessage.Context.EntityMetaData, eventMessage.Context.ObjectId, WorkFlowProcessState.Processing);
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