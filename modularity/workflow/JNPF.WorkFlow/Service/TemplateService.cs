using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Schedule;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using JNPF.VisualDev.Entitys;
using JNPF.WorkFlow.Entitys.Dto.Template;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Conifg;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Entitys.Model.WorkFlow;
using JNPF.WorkFlow.Factory;
using JNPF.WorkFlow.Interfaces.Manager;
using JNPF.WorkFlow.Interfaces.Repository;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.WorkFlow.Service;

/// <summary>
/// 流程设计.
/// </summary>
[ApiDescriptionSettings(Tag = "WorkFlow", Name = "Template", Order = 301)]
[Route("api/workflow/[controller]")]
public class TemplateService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<WorkFlowTemplateEntity> _repository;
    private readonly IWorkFlowRepository _wfRepository;
    private readonly IDictionaryDataService _dictionaryDataService;
    private readonly IUserRelationService _userRelationService;
    private readonly IWorkFlowManager _workFlowManager;
    private readonly IUserManager _userManager;
    private readonly IFileManager _fileManager;
    private readonly IBillRullService _billRuleService;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ITenant _db;

    public TemplateService(
        ISqlSugarRepository<WorkFlowTemplateEntity> repository,
        IWorkFlowRepository wfRepository,
        IDictionaryDataService dictionaryDataService,
        IUserRelationService userRelationService,
        IWorkFlowManager workFlowManager,
        IUserManager userManager,
        IFileManager fileManager,
        IBillRullService billRuleService,
        ISchedulerFactory schedulerFactory,
        ISqlSugarClient context)
    {
        _repository = repository;
        _wfRepository = wfRepository;
        _dictionaryDataService = dictionaryDataService;
        _userRelationService = userRelationService;
        _workFlowManager = workFlowManager;
        _userManager = userManager;
        _fileManager = fileManager;
        _billRuleService = billRuleService;
        _schedulerFactory = schedulerFactory;
        _db = context.AsTenant();
    }

    #region GET

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] TemplateListQuery input)
    {
        var list = await _repository.AsSugarClient().Queryable<WorkFlowTemplateEntity>()
             .Where(a => a.DeleteMark == null)
             .WhereIF(input.category.IsNotEmptyOrNull(), a => a.Category == input.category)
             .WhereIF(input.type.IsNotEmptyOrNull(), a => a.Type == input.type.ParseToInt())
             .WhereIF(input.enabledMark.IsNotEmptyOrNull(), a => a.EnabledMark == input.enabledMark)
             .WhereIF(!(_userManager.AppCode == "" || _userManager.AppCode == "workFlow"), a => a.SystemId == _userManager.SystemId)
             .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
             .Select(a => new TemplateListOutput
             {
                 id = a.Id,
                 fullName = a.FullName,
                 enCode = a.EnCode,
                 category = SqlFunc.Subqueryable<DictionaryDataEntity>().EnableTableFilter().Where(d => d.Id == a.Category).Select(d => d.FullName),
                 type = a.Type,
                 creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                 creatorTime = a.CreatorTime,
                 sortCode = a.SortCode,
                 enabledMark = a.EnabledMark,
                 lastModifyTime = a.LastModifyTime,
                 flowId = a.FlowId,
                 visibleType = a.VisibleType,
                 status = a.Status,
             }).Distinct().MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
             .OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.lastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<TemplateListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector([FromQuery] TemplateListQuery input)
    {
        var flowIds = _repository.AsSugarClient().Queryable<WorkFlowTemplateEntity>().Where(x => x.DeleteMark == null && x.Status == 1 && x.EnabledMark == 1 && !SqlFunc.IsNullOrEmpty(x.FlowId)).Select(x => x.Id).ToList();
        var userId = _userManager.UserId;
        var systemIds = new List<string>();
        if (!_userManager.IsAdministrator && input.isAuthority == 1)
        {
            if (input.systemId.IsNullOrEmpty() || input.systemId == "-1")
            {
                systemIds = await _wfRepository.GetAuthorIdList(CommonConst.AuthorizeItemSystem);
            }
            else
            {
                systemIds.Add(input.systemId);
            }
            var flowIds_myself = _wfRepository.GetFlowIdList(userId, systemIds); // 可发起流程
            if (input.isDelegate == 1)
            {
                flowIds = flowIds_myself;
            }
            else
            {
                if (flowIds_myself.IsNullOrEmpty()) flowIds_myself = new List<string>();
                var flowIds_delegate = await _wfRepository.GetDelegateFlowId(userId); // 委托给我流程
                foreach (var item in flowIds_delegate)
                {
                    if (IsDelagetelaunch(item, systemIds))
                    {
                        flowIds_myself.Add(item);
                    }
                }
                flowIds = input.systemId != "-1" ? flowIds_myself : flowIds_delegate;
            }
        }
        if (input.category.IsNotEmptyOrNull() && input.category == "commonFlow")
        {
            var commonFlowIds = _repository.AsSugarClient().Queryable<WorkFlowCommonEntity>().Where(x => x.CreatorUserId == _userManager.UserId).Select(x => x.FlowId).ToList();
            if (commonFlowIds.Any())
            {
                if (_userManager.IsAdministrator)
                {
                    flowIds = commonFlowIds;
                }
                else
                {
                    flowIds = flowIds.Intersect(commonFlowIds).ToList();
                }
                input.category = string.Empty;
            }
        }
        flowIds = flowIds.Distinct().ToList();
        var list = await _repository.AsSugarClient().Queryable<WorkFlowTemplateEntity>()
            .Where(a => a.DeleteMark == null && a.Status == 1 && a.EnabledMark == 1 && flowIds.Contains(a.Id) && a.Type != 2)
            .WhereIF(input.isLaunch == 1, a => a.ShowType != 2)
            .WhereIF(input.category.IsNotEmptyOrNull(), a => a.Category == input.category)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .WhereIF(input.systemId.IsNotEmptyOrNull() && input.systemId != "-1", a => a.SystemId == input.systemId)
            .WhereIF(input.systemId.IsNotEmptyOrNull() && input.systemId == "-1", a => !systemIds.Contains(a.SystemId))
            .Select(a => new TemplateListOutput
            {
                id = a.Id,
                creatorTime = a.CreatorTime,
                enCode = a.EnCode,
                enabledMark = a.EnabledMark,
                fullName = a.FullName,
                icon = a.Icon,
                iconBackground = a.IconBackground,
                lastModifyTime = a.LastModifyTime,
                sortCode = a.SortCode,
                type = a.Type,
                templateId = a.Id,
                flowId = a.FlowId,
                visibleType = a.VisibleType,
                isCommonFlow = SqlFunc.Subqueryable<WorkFlowCommonEntity>().EnableTableFilter().Where(c => c.FlowId == a.Id && c.CreatorUserId == _userManager.UserId).Any(),
            }).Distinct().MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.lastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        if (input.isSystemFrom == 1)
        {
            foreach (var item in list.list)
            {
                var nodeEntity = _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Where(x => x.FlowId == item.flowId && WorkFlowNodeTypeEnum.start.ParseToString().Equals(x.NodeType)).First();
                var formEntity = _repository.AsSugarClient().Queryable<VisualDevEntity>().First(x => x.Id == nodeEntity.FormId && x.DeleteMark == null);
                item.isQuote = formEntity.Type == 2 ? 1 : 0;
            }
        }
        return PageResult<TemplateListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 版本列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("Version/{id}")]
    public async Task<dynamic> GetVersionList(string id)
    {
        return await _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(a => a.TemplateId == id && a.DeleteMark == null).OrderByDescending(a => a.Status == 1 ? true : false).OrderByDescending(a => a.CreatorTime).Select(a => new {
            id = a.Id,
            fullName = SqlFunc.MergeString("流程版本(V", a.Version, ")"),
            state = a.Status
        }).ToListAsync();
    }

    /// <summary>
    /// 基础信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<TemplateInfoOutput>();
    }

    /// <summary>
    /// 模板信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("Info/{id}")]
    [UnifySerializerSetting("special")]
    public async Task<dynamic> GetJsonInfo(string id)
    {
        var versionEntity = await _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().FirstAsync(x => x.Id == id && x.DeleteMark == null);
        var nodeList = await _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Where(x => x.FlowId == id && x.DeleteMark == null).ToListAsync();
        var templateEntity = await _repository.GetFirstAsync(x => x.Id == versionEntity.TemplateId && x.DeleteMark == null);
        var output = new VersionInfoOutput();
        output.id = versionEntity.TemplateId;
        output.flowId = versionEntity.Id;
        output.flowXml = versionEntity.Xml;
        output.type = templateEntity.Type;
        output.flowNodes = nodeList.Any() ? nodeList.ToDictionary(x => x.NodeCode, y => y.NodeJson.ToObject<object>()) : new Dictionary<string, object>();
        output.fullName = templateEntity.FullName;
        output.flowConfig = templateEntity.FlowConfig;
        return output;
    }

    /// <summary>
    /// 表单主表属性.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{templateId}/FormInfo")]
    public async Task<dynamic> GetFormInfo(string templateId)
    {
        var entity = _repository.GetFirst(x => x.Id == templateId && x.EnabledMark == 1 && x.DeleteMark == null);
        var nodeEntity = _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Where(x => x.FlowId == entity.FlowId && WorkFlowNodeTypeEnum.start.ParseToString().Equals(x.NodeType)).First();
        if (nodeEntity.IsNullOrEmpty()) return null;
        var formEntity = _repository.AsSugarClient().Queryable<VisualDevEntity>().First(x => x.Id == nodeEntity.FormId && x.DeleteMark == null);
        return formEntity.Adapt<FormModel>();
    }

    /// <summary>
    /// 开始表单id.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("StartFormId/{templateId}")]
    public async Task<dynamic> GetStartFormId(string templateId)
    {
        var templateEntity = _repository.GetFirst(x => x.Id == templateId && x.EnabledMark == 1 && x.DeleteMark == null);
        if (templateEntity.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1005);
        var nodeEntity = _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Where(x => x.FlowId == templateEntity.FlowId && x.NodeType == WorkFlowNodeTypeEnum.start.ParseToString()).First();
        if (nodeEntity.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1005);
        if (!_userManager.IsAdministrator)
        {
            var flowIds = _wfRepository.GetFlowIdList(_userManager.UserId);
            if (!flowIds.Contains(templateEntity.Id)) throw Oops.Oh(ErrorCode.WF0052);
        }
        return new { flowId = templateEntity.FlowId, formId = nodeEntity.FormId };

    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string id)
    {
        var importModel = new TemplateImportOutput();
        importModel.template = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        importModel.version = _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(x => x.TemplateId == id && x.Status == 1 && x.DeleteMark == null).First();
        importModel.nodeList = _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Where(x => x.FlowId == importModel.template.FlowId && x.DeleteMark == null).ToList();
        var jsonStr = importModel.ToJsonString();
        return await _fileManager.Export(jsonStr, importModel.template.FullName, ExportFileType.ffe);
    }

    /// <summary>
    /// 所属流程列表(树形).
    /// </summary>
    /// <returns></returns>
    [HttpGet("TreeList")]
    public async Task<dynamic> GetTreeList([FromQuery] string formType)
    {
        var sysFormIdList = await _repository.AsSugarClient().Queryable<VisualDevReleaseEntity>().Where(x => x.Type == 2 && x.DeleteMark == null).Select(x => x.Id).ToListAsync();
        var flowIds = await _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Where(x => x.NodeType == WorkFlowNodeTypeEnum.start.ParseToString() && sysFormIdList.Contains(x.FormId)).Select(x => x.FlowId).ToListAsync();
        var templateList = await _repository.AsSugarClient().Queryable<WorkFlowTemplateEntity>()
            .Where(a => a.DeleteMark == null && a.Status == 1 && a.EnabledMark == 1 && a.Type != 2)
            .WhereIF(flowIds.Any(), a => !flowIds.Contains(a.FlowId))
            .WhereIF(formType.IsNotEmptyOrNull() && formType == "2", a => a.ShowType != 1)
            .WhereIF(!(_userManager.AppCode == "" || _userManager.AppCode == "workFlow"), a => a.SystemId == _userManager.SystemId)
            .Select(a => new TemplateTreeOutput
            {
                id = a.Id,
                creatorTime = a.CreatorTime,
                enCode = a.EnCode,
                fullName = a.FullName,
                icon = a.Icon,
                iconBackground = a.IconBackground,
                lastModifyTime = a.LastModifyTime,
                sortCode = a.SortCode,
                parentId = a.Category,
                category = a.Category,
            }).Distinct().MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
            .OrderBy(t => t.lastModifyTime, OrderByType.Desc).ToListAsync();
        List<TemplateTreeOutput> output = new List<TemplateTreeOutput>();
        if (templateList.Any())
        {
            var dicDataInfo = await _dictionaryDataService.GetInfo(templateList.FirstOrDefault().parentId);
            var dicDataList = await _dictionaryDataService.GetList(dicDataInfo.DictionaryTypeId);
            var dicList = new List<TemplateTreeOutput>();
            foreach (var item in dicDataList)
            {
                if (!templateList.Any(x => x.category == item.Id)) continue;
                dicList.Add(new TemplateTreeOutput()
                {
                    fullName = item.FullName,
                    parentId = "0",
                    id = item.Id,
                    disabled = true
                });
            }
            output = templateList.Union(dicList).ToList().ToTree();
        }
        return new { list = output };
    }

    /// <summary>
    /// 流程列表(在线开发、代码生成).
    /// </summary>
    /// <returns></returns>
    [HttpGet("{formId}/FlowList")]
    public async Task<dynamic> GetFlowList(string formId)
    {
        var flowIds = await _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Where(x => x.FormId == formId && x.NodeType == WorkFlowNodeTypeEnum.start.ParseToString() && x.DeleteMark == null).Select(x => x.FlowId).ToListAsync();
        if (!flowIds.Any())
        {
            return new { list = new List<object>(), isConfig = false };
        }
        if (!_userManager.IsAdministrator)
        {
            flowIds = _wfRepository.GetFlowIdList(_userManager.UserId);
        }
        var list = await _repository.AsSugarClient().Queryable<WorkFlowTemplateEntity>()
            .Where(a => a.DeleteMark == null && a.Status == 1 && a.EnabledMark == 1 && a.Type != 2)
            .WhereIF(flowIds.Any(), a => flowIds.Contains(a.FlowId))
            .WhereIF(!(_userManager.AppCode == "" || _userManager.AppCode == "workFlow"), a => a.SystemId == _userManager.SystemId)
            .Select(a => new TemplateListOutput
            {
                id = a.FlowId,
                creatorTime = a.CreatorTime,
                enCode = a.EnCode,
                enabledMark = a.EnabledMark,
                fullName = a.FullName,
                icon = a.Icon,
                iconBackground = a.IconBackground,
                lastModifyTime = a.LastModifyTime,
                sortCode = a.SortCode,
                type = a.Type,
                templateId = a.Id,
            }).Distinct().MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc).ToListAsync();
        return new { list = list, isConfig = true };
    }

    /// <summary>
    /// 子流程可发起人员.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{id}/SubFlowUserList")]
    public async Task<dynamic> GetSubFlowUserList(string id, [FromQuery] WorkFlowHandleModel input)
    {
        return _workFlowManager.GetUserIdList(id, input, 2);
    }

    /// <summary>
    /// 委托流程列表(所有流程).
    /// </summary>
    /// <returns></returns>
    [HttpPost("GetFlowList")]
    public async Task<dynamic> GetflowList([FromBody] List<string> input)
    {
        return await _repository.AsSugarClient().Queryable<WorkFlowTemplateEntity>()
            .Where(a => input.Contains(a.Id) && a.Status == 1 && a.EnabledMark == 1 && a.Type != 2)
            .WhereIF(!(_userManager.AppCode == "" || _userManager.AppCode == "workFlow"), a => a.SystemId == _userManager.SystemId)
            .Select(a => new {
                id = a.Id,
                enCode = a.EnCode,
                fullName = a.FullName,
            }).ToListAsync();
    }

    /// <summary>
    /// app常用流程(树形).
    /// </summary>
    /// <returns></returns>
    [HttpGet("CommonFlowTree")]
    public async Task<dynamic> CommonFlowTree()
    {
        var templateList = await _repository.AsSugarClient().Queryable<WorkFlowTemplateEntity, WorkFlowCommonEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.FlowId))
            .Where((a, b) => a.DeleteMark == null && a.Status == 1 && a.EnabledMark == 1 && a.Type != 2 && a.SystemId == _userManager.SystemId && b.CreatorUserId == _userManager.UserId)
            .Select(a => new TemplateTreeOutput
            {
                id = a.Id,
                creatorTime = a.CreatorTime,
                enCode = a.EnCode,
                fullName = a.FullName,
                icon = a.Icon,
                iconBackground = a.IconBackground,
                lastModifyTime = a.LastModifyTime,
                sortCode = a.SortCode,
                parentId = a.Category,
                category = a.Category,
            }).Distinct().MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
            .OrderBy(t => t.lastModifyTime, OrderByType.Desc).ToListAsync();
        List<TemplateTreeOutput> output = new List<TemplateTreeOutput>();
        if (templateList.Any())
        {
            var dicDataInfo = await _dictionaryDataService.GetInfo(templateList.FirstOrDefault().parentId);
            var dicDataList = await _dictionaryDataService.GetList(dicDataInfo.DictionaryTypeId);
            var dicList = new List<TemplateTreeOutput>();
            foreach (var item in dicDataList)
            {
                if (templateList.Any(x => x.category == item.Id))
                {
                    dicList.Add(new TemplateTreeOutput()
                    {
                        fullName = item.FullName,
                        parentId = "0",
                        id = item.Id,
                        disabled = true
                    });
                }
            }
            output = templateList.Union(dicList).ToList().ToTree();
        }
        output.Insert(0, new TemplateTreeOutput()
        {
            fullName = "全部流程",
            parentId = "0",
            id = "-1",
            disabled = true,
            children = templateList.ToObject<List<object>>()
        });

        return new { list = output };
    }

    /// <summary>
    /// 常用流程.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("getCommonFlowList")]
    public async Task<dynamic> getCommonFlowList([FromQuery] KeywordInput input)
    {
        var templateList = await _repository.AsSugarClient().Queryable<WorkFlowTemplateEntity, WorkFlowCommonEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.FlowId))
            .Where((a, b) => a.DeleteMark == null && a.Status == 1 && a.EnabledMark == 1 && a.Type != 2 && b.CreatorUserId == _userManager.UserId)
            .WhereIF(_userManager.AppCode.IsNotEmptyOrNull(), a => a.SystemId.Equals(_userManager.SystemId))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword))
            .Select(a => new TemplateTreeOutput
            {
                id = a.Id,
                creatorTime = a.CreatorTime,
                enCode = a.EnCode,
                fullName = a.FullName,
                icon = a.Icon,
                iconBackground = a.IconBackground,
                lastModifyTime = a.LastModifyTime,
                sortCode = a.SortCode,
                parentId = a.Category,
                category = a.Category,
            }).Distinct().MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
            .OrderBy(t => t.lastModifyTime, OrderByType.Desc).ToListAsync();
        return new { list = templateList };
    }
    #endregion

    #region POST

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task<dynamic> Create([FromBody] TemplateInfoOutput input)
    {
        if (await _repository.IsAnyAsync(x => (x.EnCode == input.enCode || (x.FullName == input.fullName && x.SystemId.Equals(_userManager.SystemId))) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var templateEntity = input.Adapt<WorkFlowTemplateEntity>();
        templateEntity.Id = SnowflakeIdHelper.NextId();
        templateEntity.EnabledMark = 0;
        templateEntity.Status = 0;
        templateEntity.SystemId = _userManager.SystemId;
        templateEntity.EnCode = templateEntity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeWorkFlow) : templateEntity.EnCode;
        var versionEntity = new WorkFlowVersionEntity();
        versionEntity.TemplateId = templateEntity.Id;
        versionEntity.Version = "1";
        versionEntity.Status = 0;
        await _repository.AsSugarClient().Insertable(versionEntity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
        var result = await _repository.AsSugarClient().Insertable(templateEntity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
        if (result == null)
            throw Oops.Oh(ErrorCode.COM1000);
        return result.Id;
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] TemplateInfoOutput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && (x.EnCode == input.enCode || (x.FullName == input.fullName && x.SystemId.Equals(_userManager.SystemId))) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var templateEntity = input.Adapt<WorkFlowTemplateEntity>();
        templateEntity.SystemId = _userManager.SystemId;
        templateEntity.EnCode = templateEntity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeWorkFlow) : templateEntity.EnCode;
        var isOk = await _repository.AsSugarClient().Updateable(templateEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 更新类型.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPut("{id}/UpdateType")]
    public async Task UpdateType(string id)
    {
        var isOk = await _repository.AsUpdateable().SetColumns(it => new WorkFlowTemplateEntity()
        {
            Type = 0,
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id.Equals(id)).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1003);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var templateEntity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (templateEntity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var versionList = _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(x => x.TemplateId == id).ToList();
        var flowIdList = versionList.Select(x => x.Id).ToList();
        var flowableIds = string.Join(",", versionList.FindAll(x => x.FlowableId.IsNotEmptyOrNull()).Select(x => x.FlowableId).ToList());
        if (await _repository.AsSugarClient().Queryable<WorkFlowTaskEntity>().AnyAsync(x => x.DeleteMark == null && flowIdList.Contains(x.FlowId)))
            throw Oops.Oh(ErrorCode.WF0037);
        if (await _repository.AsSugarClient().Queryable<WorkFlowTriggerTaskEntity>().AnyAsync(x => x.DeleteMark == null && flowIdList.Contains(x.FlowId)))
            throw Oops.Oh(ErrorCode.WF0037);
        _db.BeginTran();
        if (flowableIds.IsNotEmptyOrNull())
        {
            await BpmnEngineFactory.CreateBmpnEngine().DefinitionDelete(flowableIds);
        }
        _repository.AsSugarClient().Deleteable<WorkFlowVersionEntity>().Where(it => flowIdList.Contains(it.Id)).ExecuteCommandHasChange();
        _repository.AsSugarClient().Deleteable<WorkFlowNodeEntity>().Where(x => flowIdList.Contains(x.FlowId)).ExecuteCommandHasChange();
        var isOk = await _repository.AsUpdateable(templateEntity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        _db.CommitTran();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 保存.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Save")]
    public async Task Save([FromBody] VersionInfoOutput input)
    {
        try
        {
            _db.BeginTran();
            var sendConfigIdList = new List<string>();
            var templateEntity = await _repository.GetFirstAsync(x => x.Id == input.id && x.DeleteMark == null);
            var versionEntity = await _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().FirstAsync(x => x.Id == input.flowId && x.DeleteMark == null);
            templateEntity.FlowConfig = input.flowConfig;
            templateEntity.VisibleType = input.flowConfig.ToObject<FlowConfig>().visibleType;
            TriggerProperties timePro = null;
            if (versionEntity.Status == 0 || versionEntity.Status == 1)
            {
                versionEntity.Xml = input.flowXml;
                var nodeList = new List<WorkFlowNodeEntity>();
                var formId = string.Empty;
                foreach (var item in input.flowNodes.Keys)
                {
                    var nodeProperties = input.flowNodes[item].ToObject<NodeProperties>();
                    if (WorkFlowNodeTypeEnum.start.ToString().Equals(nodeProperties.type))
                    {
                        formId = nodeProperties.formId;
                        break;
                    }
                }
                foreach (var item in input.flowNodes.Keys)
                {
                    var node = new WorkFlowNodeEntity();
                    var nodeProperties = input.flowNodes[item].ToObject<NodeProperties>();
                    node.FlowId = versionEntity.Id;
                    node.NodeCode = item;
                    node.NodeType = nodeProperties.type;
                    node.FormId = nodeProperties.formId.IsNotEmptyOrNull() ? nodeProperties.formId : formId;
                    if (node.NodeType.ToLower().Contains("trigger"))
                    {
                        var triggerPro = input.flowNodes[item].ToObject<TriggerProperties>();
                        if (node.NodeType == "noticeTrigger")
                        {
                            node.FormId = triggerPro.noticeId;
                        }
                        if (node.NodeType == "webhookTrigger")
                        {
                            node.FormId = versionEntity.Id;
                        }
                        if (node.NodeType == "timeTrigger")
                        {
                            node.FormId = versionEntity.Id;
                            timePro = triggerPro;
                        }
                    }
                    node.NodeJson = input.flowNodes[item].ToJsonStringOld();
                    GetSendConfigIdList(nodeProperties, sendConfigIdList);
                    nodeList.Add(node);
                }
                if (nodeList.Any())
                {
                    await _repository.AsSugarClient().Deleteable<WorkFlowNodeEntity>().Where(x => x.FlowId == versionEntity.Id).ExecuteCommandAsync();
                    await _repository.AsSugarClient().Insertable(nodeList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }
                versionEntity.SendConfigIds = sendConfigIdList.ToJsonString();
                // 发布版本
                if (input.type == 1)
                {
                    _repository.AsSugarClient().Updateable<WorkFlowVersionEntity>().SetColumns(it => new WorkFlowVersionEntity()
                    {
                        Status = 2,
                        LastModifyUserId = _userManager.UserId,
                        LastModifyTime = SqlFunc.GetDate()
                    }).Where(it => it.Id == templateEntity.FlowId && it.Status == 1).ExecuteCommandHasChange();
                    versionEntity.Status = 1;
                    #region 部署flowable
                    var flowableId = await BpmnEngineFactory.CreateBmpnEngine().DefinitionDeploy(versionEntity.Xml);
                    versionEntity.FlowableId = flowableId;
                    #endregion
                    templateEntity.EnabledMark = 1;
                    templateEntity.Status = 1;
                    templateEntity.FlowId = versionEntity.Id;
                    templateEntity.FlowableId = versionEntity.FlowableId;
                    templateEntity.Version = versionEntity.Version;
                }
                await _repository.AsSugarClient().Updateable(versionEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
                await _repository.AsSugarClient().Updateable(templateEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
            }
            else if (versionEntity.Status == 2)
            {
                _repository.AsSugarClient().Updateable<WorkFlowVersionEntity>().SetColumns(it => new WorkFlowVersionEntity()
                {
                    Status = 2,
                    LastModifyUserId = _userManager.UserId,
                    LastModifyTime = SqlFunc.GetDate()
                }).Where(it => it.Id == templateEntity.FlowId && it.Status == 1).ExecuteCommandHasChange();
                versionEntity.Status = 1;
                templateEntity.FlowId = versionEntity.Id;
                templateEntity.FlowableId = versionEntity.FlowableId;
                templateEntity.Version = versionEntity.Version;
                await _repository.AsSugarClient().Updateable(versionEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
                await _repository.AsSugarClient().Updateable(templateEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
            }
            _db.CommitTran();
            var flowIds = await _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(x => x.TemplateId == templateEntity.Id && x.Id != versionEntity.Id && x.DeleteMark == null).Select(x => x.Id).ToListAsync();
            if (flowIds.Any())
            {
                flowIds = flowIds.Select(x => string.Format("JobTaskFlow_Http_{0}_{1}", _userManager.TenantId, x)).ToList();
                _schedulerFactory.RemoveJob(flowIds.ToArray());
            }
            if (templateEntity.Type == 2 && input.type == 1 && timePro != null)
            {
                await _workFlowManager.TimeTriggerTask(timePro, versionEntity.Id);
                if (templateEntity.Status != 1)
                {
                    _schedulerFactory.PauseJob(string.Format("JobTaskFlow_Http_{0}_{1}", _userManager.TenantId, versionEntity.Id));
                }
            }
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 复制.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Copy")]
    public async Task ActionsCopy(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var random = RandomExtensions.NextLetterAndNumberString(new Random(), 5).ToLower();
        entity.FullName = string.Format("{0}.副本{1}", entity.FullName, random);
        entity.EnCode = string.Format("{0}{1}", entity.EnCode, random);
        if (entity.FullName.Length >= 50 || entity.EnCode.Length >= 50)
            throw Oops.Oh(ErrorCode.COM1009);
        entity.LastModifyTime = null;
        entity.LastModifyUserId = null;
        entity.CreatorUserId = _userManager.UserId;
        entity.EnabledMark = 0;
        entity.Status = 0;
        entity.Id = SnowflakeIdHelper.NextId();
        await CreateInfo(entity.FlowId, entity.Id);
        entity.FlowId = string.Empty;
        entity.FlowableId = string.Empty;
        var result = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
        if (result == null)
            throw Oops.Oh(ErrorCode.COM1005);
    }

    /// <summary>
    /// 删除版本.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("info/{id}")]
    public async Task DeleteInfo(string id)
    {
        if (await _repository.AsSugarClient().Queryable<WorkFlowTaskEntity>().AnyAsync(x => x.DeleteMark == null && x.FlowId == id))
            throw Oops.Oh(ErrorCode.WF0037);
        var jsonInfo = _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().First(x => x.Id == id && x.DeleteMark == null);
        _repository.AsSugarClient().Deleteable<WorkFlowNodeEntity>().Where(x => x.FlowId == id).ExecuteCommandHasChange();
        var isOk = _repository.AsSugarClient().Deleteable<WorkFlowVersionEntity>().Where(it => it.Id == id).ExecuteCommandHasChange();
        if (jsonInfo.FlowableId.IsNotEmptyOrNull())
        {
            await BpmnEngineFactory.CreateBmpnEngine().DefinitionDelete(jsonInfo.FlowableId);
        }
        _schedulerFactory.RemoveJob(string.Format("JobTaskFlow_Http_{0}_{1}", _userManager.TenantId, id));
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1003);
    }

    /// <summary>
    /// 新增版本.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("info/{id}")]
    public async Task<dynamic> CreateInfo(string id, string templateId)
    {
        var oldVersionEntity = _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().First(x => x.Id == id && x.DeleteMark == null);
        var nodeList = _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Where(x => x.FlowId == oldVersionEntity.Id && x.DeleteMark == null).ToList();
        var maxVersion = _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(x => x.TemplateId == oldVersionEntity.TemplateId).OrderBy(x => SqlFunc.ToInt64(x.Version), OrderByType.Desc).First();
        if (maxVersion.IsNotEmptyOrNull() && maxVersion.Version.IsNotEmptyOrNull())
        {
            oldVersionEntity.Version = (maxVersion.Version.ParseToInt() + 1).ToString();
        }
        else
        {
            if (oldVersionEntity.Version.IsNullOrEmpty()) oldVersionEntity.Version = "1";
            oldVersionEntity.Version = (oldVersionEntity.Version.ParseToInt() + 1).ToString();
        }
        oldVersionEntity.Status = 0;
        oldVersionEntity.Id = SnowflakeIdHelper.NextId();
        oldVersionEntity.FlowableId = null;
        if (nodeList.Any())
        {
            foreach (var node in nodeList)
            {
                node.FlowId = oldVersionEntity.Id;
                if (node.NodeType == WorkFlowNodeTypeEnum.webhookTrigger.ToString())
                {
                    var nodePro = node.NodeJson.ToObject<Dictionary<string, object>>();
                    if (nodePro != null && nodePro.ContainsKey("webhookUrl")) { nodePro["webhookUrl"] = ""; }
                    node.NodeJson = nodePro.ToJsonStringOld();
                }
            }
            await _repository.AsSugarClient().Insertable(nodeList).CallEntityMethod(m => m.Creator()).ExecuteReturnEntityAsync();
        }
        if (templateId.IsNotEmptyOrNull())
        {
            oldVersionEntity.Version = "1";
            oldVersionEntity.TemplateId = templateId;
        }
        var result = await _repository.AsSugarClient().Insertable(oldVersionEntity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
        if (result == null)
            throw Oops.Oh(ErrorCode.COM1000);
        return result.Id;
    }

    /// <summary>
    /// 导入.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("Actions/Import")]
    public async Task ActionsImport(string type, IFormFile file)
    {
        var fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.ffe.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        TemplateImportOutput model;
        try
        {
            model = josn.ToObject<TemplateImportOutput>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }
        if (model == null)
            throw Oops.Oh(ErrorCode.D3006);
        await ImportDataRepeat(model, type);
        await ImportData(model);
    }

    /// <summary>
    /// 常用流程.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPost("SetCommonFlow/{id}")]
    public async Task SetCommonFlow(string id)
    {
        if (_repository.AsSugarClient().Queryable<WorkFlowCommonEntity>().Any(x => x.FlowId == id && x.CreatorUserId == _userManager.UserId))
        {
            await _repository.AsSugarClient().Deleteable<WorkFlowCommonEntity>().Where(x => x.FlowId == id && x.CreatorUserId == _userManager.UserId).ExecuteCommandAsync();
        }
        else
        {
            var workFlowCommonEntity = new WorkFlowCommonEntity();
            workFlowCommonEntity.FlowId = id;
            await _repository.AsSugarClient().Insertable(workFlowCommonEntity).CallEntityMethod(m => m.Creator()).ExecuteReturnEntityAsync();
        }
    }

    /// <summary>
    /// 上下架.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPut("{id}/UpDownShelf")]
    public async Task UpDownShelf(string id, [FromBody] TemplateUpDownInput input)
    {
        var status = input.isUp == 0 ? 1 : input.isHidden == 0 ? 2 : 3;
        var isOk = await _repository.AsUpdateable().SetColumns(it => new WorkFlowTemplateEntity()
        {
            Status = status,
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id.Equals(id)).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1003);
        var templateEntity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (templateEntity.Type == 2)
        {
            var versionEntity = await _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().FirstAsync(x => x.Id == templateEntity.FlowId && x.DeleteMark == null);
            var node = await _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().FirstAsync(x => x.FlowId == versionEntity.Id && x.NodeType == "timeTrigger" && x.DeleteMark == null);
            if (node.IsNotEmptyOrNull())
            {
                if (status == 1)
                {
                    _schedulerFactory.StartJob(string.Format("JobTaskFlow_Http_{0}_{1}", _userManager.TenantId, versionEntity.Id));
                }
                else
                {
                    _schedulerFactory.PauseJob(string.Format("JobTaskFlow_Http_{0}_{1}", _userManager.TenantId, versionEntity.Id));
                }
            }
        }
    }

    #endregion

    #region PublicMethod
    #endregion

    #region PrivateMethod

    /// <summary>
    /// 获取消息配置Id.
    /// </summary>
    /// <param name="template"></param>
    /// <param name="sendConfigIdList"></param>
    private void GetSendConfigIdList(NodeProperties nodeProperties, List<string> sendConfigIdList)
    {
        if (nodeProperties.IsNotEmptyOrNull() && !WorkFlowNodeTypeEnum.global.ToString().Equals(nodeProperties.type))
        {
            if (nodeProperties.approveMsgConfig.IsNotEmptyOrNull() && nodeProperties.approveMsgConfig.msgId.IsNotEmptyOrNull()) sendConfigIdList.Add(nodeProperties.approveMsgConfig.msgId);
            if (nodeProperties.rejectMsgConfig.IsNotEmptyOrNull() && nodeProperties.rejectMsgConfig.msgId.IsNotEmptyOrNull()) sendConfigIdList.Add(nodeProperties.rejectMsgConfig.msgId);
            if (nodeProperties.backMsgConfig.IsNotEmptyOrNull() && nodeProperties.backMsgConfig.msgId.IsNotEmptyOrNull()) sendConfigIdList.Add(nodeProperties.backMsgConfig.msgId);
            if (nodeProperties.copyMsgConfig.IsNotEmptyOrNull() && nodeProperties.copyMsgConfig.msgId.IsNotEmptyOrNull()) sendConfigIdList.Add(nodeProperties.copyMsgConfig.msgId);
            if (nodeProperties.overTimeMsgConfig.IsNotEmptyOrNull() && nodeProperties.overTimeMsgConfig.msgId.IsNotEmptyOrNull()) sendConfigIdList.Add(nodeProperties.overTimeMsgConfig.msgId);
            if (nodeProperties.noticeMsgConfig.IsNotEmptyOrNull() && nodeProperties.noticeMsgConfig.msgId.IsNotEmptyOrNull()) sendConfigIdList.Add(nodeProperties.noticeMsgConfig.msgId);
            if (nodeProperties.launchMsgConfig.IsNotEmptyOrNull() && nodeProperties.launchMsgConfig.msgId.IsNotEmptyOrNull()) sendConfigIdList.Add(nodeProperties.launchMsgConfig.msgId);
            if (nodeProperties.waitMsgConfig.IsNotEmptyOrNull() && nodeProperties.waitMsgConfig.msgId.IsNotEmptyOrNull()) sendConfigIdList.Add(nodeProperties.waitMsgConfig.msgId);
            sendConfigIdList.Remove("");
            sendConfigIdList = sendConfigIdList.Distinct().ToList();
        }
    }

    /// <summary>
    /// 导入验证.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="type">识别重复(0-跳过 1-追加).</param>
    /// <returns></returns>
    private async Task ImportDataRepeat(TemplateImportOutput model, string type)
    {
        var errorMainList = new List<string>();
        var errorChildList = new List<string>();
        var errorChild1List = new List<string>();
        var random = RandomExtensions.NextLetterAndNumberString(new Random(), 5).ToLower();

        if (model.template.SystemId.IsNullOrEmpty() || !model.template.SystemId.Equals(_userManager.SystemId))
        {
            model.template.Id = SnowflakeIdHelper.NextId();
            model.template.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeWorkFlow);
            model.version.Id = SnowflakeIdHelper.NextId();
            model.version.TemplateId = model.template.Id;
            foreach (var node in model.nodeList)
            {
                node.Id = SnowflakeIdHelper.NextId();
                node.FlowId = model.version.Id;
            }
        }

        if (await _repository.IsAnyAsync(x => x.Id == model.template.Id && x.DeleteMark == null))
        {
            errorMainList.Add("ID");
        }
        if (await _repository.IsAnyAsync(x => x.EnCode == model.template.EnCode && x.DeleteMark == null))
        {
            errorMainList.Add("编码");
        }
        if (await _repository.IsAnyAsync(x => x.FullName == model.template.FullName && x.SystemId.Equals(_userManager.SystemId) && x.DeleteMark == null))
        {
            errorMainList.Add("名称");
        }
        if (await _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().AnyAsync(x => x.Id == model.version.Id))
        {
            var str = string.Format("ID({0})", model.version.Id);
            errorChildList.Add(str);
        }

        var nodeList = await _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Select(x => x.Id).ToListAsync();
        var nodeIds = nodeList.Intersect(model.nodeList.Select(x => x.Id)).ToList();
        if (nodeIds.Any())
        {
            var str = string.Format("ID({0})", string.Join("、", nodeIds));
            errorChild1List.Add(str);
        }

        if (type == "0")
        {
            var error = string.Empty;
            if (errorMainList.Any())
            {
                error += string.Format("{0}重复；", string.Join("、", errorMainList));
            }
            if (errorChildList.Any())
            {
                error += string.Format("流程版本：{0}重复；", string.Join("、", errorChildList));
            }
            if (errorChild1List.Any())
            {
                error += string.Format("流程节点：{0}重复；", string.Join("、", errorChild1List));
            }
            if (error.IsNotEmptyOrNull())
            {
                throw Oops.Oh(ErrorCode.COM1018, error);
            }
        }
        else
        {
            if (errorMainList.Any())
            {
                model.template.Id = SnowflakeIdHelper.NextId();
                model.template.EnCode = string.Format("{0}{1}", model.template.EnCode, random);
                model.template.FullName = string.Format("{0}.副本{1}", model.template.FullName, random);
            }
            model.version.Id = SnowflakeIdHelper.NextId();
            foreach (var item in model.nodeList)
            {
                item.Id = SnowflakeIdHelper.NextId();
            }
        }
    }

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="model">导入实例.</param>
    /// <returns></returns>
    private async Task ImportData(TemplateImportOutput model)
    {
        try
        {
            _db.BeginTran();

            model.template.CreatorTime = DateTime.Now;
            model.template.CreatorUserId = _userManager.UserId;
            model.template.LastModifyTime = null;
            model.template.LastModifyUserId = null;
            model.template.EnabledMark = 0;
            model.template.SystemId = _userManager.SystemId;
            model.template.Status = 0;
            await _repository.AsSugarClient().Storageable(model.template).WhereColumns(it => it.Id).ExecuteCommandAsync();

            model.version.TemplateId = model.template.Id;
            model.version.CreatorTime = DateTime.Now;
            model.version.CreatorUserId = _userManager.UserId;
            model.version.LastModifyTime = null;
            model.version.LastModifyUserId = null;
            model.version.Status = 0;
            model.version.Version = "1";
            await _repository.AsSugarClient().Storageable(model.version).WhereColumns(it => it.Id).ExecuteCommandAsync();

            model.nodeList.ForEach(x =>
            {
                x.FlowId = model.version.Id;
                x.CreatorTime = DateTime.Now;
                x.CreatorUserId = _userManager.UserId;
                x.LastModifyTime = null;
                x.LastModifyUserId = null;
            });
            await _repository.AsSugarClient().Storageable(model.nodeList).WhereColumns(it => it.Id).ExecuteCommandAsync();

            _db.CommitTran();
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw Oops.Oh(ErrorCode.D3006);
        }
    }

    /// <summary>
    /// 委托人是否有权限.
    /// </summary>
    /// <param name="templateId"></param>
    /// <returns></returns>
    private bool IsDelagetelaunch(string templateId, List<string> systemIds)
    {
        var entity = _repository.GetFirst(x => x.Id == templateId && x.DeleteMark == null);
        if (entity.IsNullOrEmpty()) return false;
        var flowInfo = _wfRepository.GetFlowInfo(entity.FlowId);
        if (flowInfo.IsNullOrEmpty()) return false;
        // 选择流程
        var delegateList = _wfRepository.GetDelegateList(x => x.Type == 0 && !SqlFunc.IsNullOrEmpty(x.FlowId) && x.FlowId.Contains(flowInfo.templateId) && x.EndTime > DateTime.Now && x.StartTime < DateTime.Now && x.DeleteMark == null);
        // 全部流程
        var delegateListAll = _wfRepository.GetDelegateList(x => x.Type == 0 && x.FlowName == "全部流程" && x.EndTime > DateTime.Now && x.StartTime < DateTime.Now && x.DeleteMark == null);
        if (flowInfo.visibleType == 2)
        {
            // 当前流程可发起人员
            var objlist = _wfRepository.GetObjIdList(flowInfo.templateId);
            var userIdList = _userRelationService.GetUserId(objlist);
            delegateList = delegateList.FindAll(x => userIdList.Contains(x.UserId));
            delegateListAll = delegateListAll.FindAll(x => userIdList.Contains(x.UserId));
        }
        var delegateIds = delegateList.Union(delegateListAll).Select(x => x.Id).Distinct().ToList();
        // 委托/代理给当前用户的数据
        var delegateInfos = _wfRepository.GetDelegateInfoList(x => x.ToUserId == _userManager.UserId && x.Status == 1 && delegateIds.Contains(x.DelegateId));
        return delegateInfos.Any() && systemIds.Contains(entity.SystemId);
    }
    #endregion
}