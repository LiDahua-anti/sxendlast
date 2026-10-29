using JNPF.WorkFlow.Interfaces.Manager;

namespace JNPF.WorkFlow.Factory;

public static class BpmnEngineFactory
{
    public static IBpmnEngine CreateBmpnEngine()
    {
        return new FlowableUtil();
    }
}
