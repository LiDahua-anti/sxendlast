using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualDev;
namespace JNPF.VisualDev.Interfaces;

/// <summary>
/// 可视化开发基础抽象类.
/// </summary>
public interface IVisualDevService
{
    /// <summary>
    /// 获取功能信息.
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <param name="isGetRelease">是否获取发布版本.</param>
    /// <returns></returns>
    Task<VisualDevEntity> GetInfoById(string id, bool isGetRelease = false);

    /// <summary>
    /// 获取代码生成命名规范.
    /// </summary>
    /// <param name="modelId">主键ID.</param>
    /// <returns></returns>
    Task<List<VisualAliasEntity>> GetAliasList(string modelId);

    /// <summary>
    /// 新增导入数据.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="type">识别重复（0：跳过，1：追加）.</param>
    /// <returns></returns>
    Task CreateImportData(VisualDevExportOutput input, int type);
}
