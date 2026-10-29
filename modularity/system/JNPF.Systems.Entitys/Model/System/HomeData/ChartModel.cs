using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.HomeData;

/// <summary>
/// 图状模型.
/// </summary>
[SuppressSniffer]
public class ChartBaseModel
{
    public string name { get; set; }
}

/// <summary>
/// 饼状.
/// </summary>
public class PieChartModel : ChartBaseModel
{
    public int data { get; set; }
}

/// <summary>
/// 柱状.
/// </summary>
public class ColumnChartModel : ChartBaseModel
{
    public object category { get; set; }
    public object data { get; set; }
}
