using JNPF.Systems.Entitys.Model.DataSet;
using JNPF.Systems.Entitys.Model.PrintDev;
using JNPF.Systems.Entitys.System;
using SqlSugar;

namespace JNPF.Systems.Interfaces.System;

/// <summary>
/// 数据集接口.
/// </summary>
public interface IDataSetService
{
    /// <summary>
    /// 获取字段模型.
    /// </summary>
    /// <param name="dbLinkId"></param>
    /// <param name="sql"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    Task<List<DataSetFieldModel>> GetFieldModels(string dbLinkId, string sql, List<SugarParameter> parameter);

    /// <summary>
    /// 保存数据集列表.
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="objectType"></param>
    /// <param name="modelList"></param>
    /// <returns></returns>
    Task SaveDataSetList(string objectId, string objectType, List<PrintDevDataSetModel> modelList);

    /// <summary>
    /// 获取配置式sql.
    /// </summary>
    /// <param name="dbLinkId"></param>
    /// <param name="visualConfigJson"></param>
    /// <param name="filterConfig"></param>
    /// <returns></returns>
    Task<string> GetVisualConfigSql(string dbLinkId, string visualConfigJson, FilterConfigModel filterConfig);

    /// <summary>
    /// 获取数据集数据.
    /// </summary>
    /// <param name="dbLinkId"></param>
    /// <param name="sql"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    Task<List<Dictionary<string, object>>> GetDataSetData(string dbLinkId, string sql, List<SugarParameter> parameter);

    /// <summary>
    /// 转换数据.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="dataSet"></param>
    /// <param name="convertConfigJson"></param>
    /// <returns></returns>
    Task ConvertData(List<Dictionary<string, object>> list, DataSetEntity dataSet, string convertConfigJson);

    /// <summary>
    /// 结果集筛选.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="filterConfig"></param>
    /// <returns></returns>
    List<Dictionary<string, object>> ResultFiltering(List<Dictionary<string, object>> list, FilterConfigModel filterConfig);
}