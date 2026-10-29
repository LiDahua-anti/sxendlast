using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Model.WorkFlow;
using Mapster;

namespace JNPF.WorkFlow.Entitys.Mapper;

internal class Mapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<WorkFlowNodeRecordEntity, ProgressModel>()
           .Map(dest => dest.startTime, src => src.CreatorTime);
        config.ForType<WorkFlowTaskEntity, TaskModel>()
           .Map(dest => dest.flowUrgent, src => src.Urgent);
    }
}
