using JNPF.Common.Dtos.VisualDev;
using JNPF.Common.Filter;
using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Entitys.Model;

namespace JNPF.VisualDev.Interfaces;

/// <summary>
/// 在线开发运行服务接口.
/// </summary>
public interface IRunService
{
    /// <summary>
    /// 创建在线开发功能.
    /// </summary>
    /// <param name="templateEntity">功能模板实体.</param>
    /// <param name="dataInput">数据输入.</param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    Task Create(VisualDevEntity templateEntity, VisualDevModelDataCrInput dataInput, string? tenantId = null);

    /// <summary>
    /// 创建在线开发有表SQL.
    /// </summary>
    /// <param name="templateEntity"></param>
    /// <param name="dataInput"></param>
    /// <param name="mainId"></param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    Task<Dictionary<string, List<Dictionary<string, object>>>> CreateHaveTableSql(VisualDevEntity templateEntity, VisualDevModelDataCrInput dataInput, string mainId, string? tenantId = null);

    /// <summary>
    /// 修改在线开发功能.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="templateEntity"></param>
    /// <param name="visualdevModelDataUpForm"></param>
    /// <returns></returns>
    Task Update(string id, VisualDevEntity templateEntity, VisualDevModelDataUpInput visualdevModelDataUpForm);

    /// <summary>
    /// 修改在线开发有表sql.
    /// </summary>
    /// <param name="templateEntity"></param>
    /// <param name="dataInput"></param>
    /// <param name="mainId"></param>
    /// <param name="logList"></param>
    /// <returns></returns>
    Task<List<string>> UpdateHaveTableSql(VisualDevEntity templateEntity, VisualDevModelDataUpInput dataInput, string mainId, List<VisualLogModel>? logList = null);

    /// <summary>
    /// 删除有表信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    Task DelHaveTableInfo(string id, VisualDevEntity templateEntity, string flowId = "");

    /// <summary>
    /// 删除子表数据.
    /// </summary>
    /// <param name="templateEntity"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    Task DelChildTable(VisualDevEntity templateEntity, VisualDevModelDelChildTableInput input);

    /// <summary>
    /// 批量删除有表数据.
    /// </summary>
    /// <param name="ids">id数组.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="visualdevModelDataDeForm"></param>
    /// <returns></returns>
    Task BatchDelHaveTableData(List<string> ids, VisualDevEntity templateEntity, VisualDevModelDataBatchDelInput? visualdevModelDataDeForm = null);

    /// <summary>
    /// 列表数据处理.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="input"></param>
    /// <param name="actionType"></param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    Task<PageResult<Dictionary<string, object>>> GetListResult(VisualDevEntity entity, VisualDevModelListQueryInput input, string actionType = "List", string? tenantId = null);

    /// <summary>
    /// 关联表单列表数据处理.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="input"></param>
    /// <param name="actionType"></param>
    /// <returns></returns>
    Task<PageResult<Dictionary<string, object>>> GetRelationFormList(VisualDevEntity entity, VisualDevModelListQueryInput input, string actionType = "List");

    /// <summary>
    /// 获取有表详情.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="flowId">流程id.</param>
    /// <param name="isConvert">是否转换数据.</param>
    /// <param name="menuId">菜单id.</param>
    /// <returns></returns>
    Task<Dictionary<string, object>> GetHaveTableInfo(string id, VisualDevEntity templateEntity, string? flowId = null, bool isConvert = false, string? menuId = null);

    /// <summary>
    /// 获取有表详情转换.
    /// </summary>
    /// <param name="templateEntity"></param>
    /// <param name="id"></param>
    /// <param name="propsValue"></param>
    /// <param name="menuId"></param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    Task<string> GetHaveTableInfoDetails(VisualDevEntity templateEntity, string id, string? propsValue = null, string? menuId = null, string? tenantId = null);

    /// <summary>
    /// 生成系统自动生成字段.
    /// </summary>
    /// <param name="moduleId">模板id.</param>
    /// <param name="fieldsModelListJson">模板数据.</param>
    /// <param name="allDataMap">真实数据.</param>
    /// <param name="IsCreate">创建与修改标识 true创建 false 修改.</param>
    /// <param name="systemControlList">不赋值的系统控件Key.</param>
    /// <returns></returns>
    Task<Dictionary<string, object>> GenerateFeilds(string moduleId, string fieldsModelListJson, Dictionary<string, object> allDataMap, bool IsCreate, List<string>? systemControlList = null);

    /// <summary>
    /// 获取数据库连接,根据linkId.
    /// </summary>
    /// <param name="linkId">数据库连接Id.</param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    Task<DbLinkEntity> GetDbLink(string linkId, string? tenantId = null);

    /// <summary>
    /// 添加、修改 流程表单数据.
    /// </summary>
    /// <param name="entity">表单模板.</param>
    /// <param name="formData">表单数据json.</param>
    /// <param name="dataId">主键Id.</param>
    /// <param name="flowId">流程引擎主键Id.</param>
    /// <param name="isUpdate">是否修改.</param>
    /// <param name="systemControlList">不赋值的系统控件Key.</param>
    /// <returns></returns>
    Task<List<VisualLogModel>> SaveFlowFormData(VisualDevReleaseEntity entity, string formData, string dataId, string flowId, bool isUpdate = false, List<string>? systemControlList = null);

    /// <summary>
    /// 获取或删除流程表单数据.
    /// </summary>
    /// <param name="id">表单模板id.</param>
    /// <param name="dataId">主键Id.</param>
    /// <param name="isDelete">是否删除（0:详情，1:删除）.</param>
    /// <param name="flowId">流程id.</param>
    /// <param name="isConvert">是否转换数据.</param>
    /// <returns></returns>
    Task<Dictionary<string, object>?> GetOrDelFlowFormData(string id, string dataId, int isDelete, string flowId = "", bool isConvert = false);

    /// <summary>
    /// 流程表单数据传递.
    /// </summary>
    /// <param name="startFId">起始表单模板Id.</param>
    /// <param name="lastFId">上节点表单模板Id.</param>
    /// <param name="newFId">传递表单模板Id.</param>
    /// <param name="mapRule">映射规则字段 : Key 原字段, Value 映射字段.</param>
    /// <param name="allFormData">所有表单数据.</param>
    /// <param name="isSubFlow">是否子流程.</param>
    Task<Dictionary<string, object>> SaveDataToDataByFId(string startFId, string lastFId, string newFId, List<Dictionary<string, string>> mapRule, Dictionary<string, object> allFormData, bool isSubFlow = false);

    /// <summary>
    /// 更新流程数据状态.
    /// </summary>
    /// <param name="formIdList"></param>
    /// <param name="flowTaskId"></param>
    /// <param name="flowState"></param>
    /// <returns></returns>
    Task UpdateFlowState(List<string> formIdList, string flowTaskId, int flowState);

    /// <summary>
    /// 处理模板默认值 (针对流程表单).
    /// 用户选择 , 部门选择 , 岗位选择 , 角色选择 , 分组选择.
    /// </summary>
    /// <param name="propertyJson">表单json.</param>
    /// <param name="tableJson">关联表单.</param>
    /// <param name="formType">表单类型（1：系统表单 2：自定义表单）.</param>
    /// <returns></returns>
    string GetVisualDevModelDataConfig(string propertyJson, string tableJson, int formType);
}
