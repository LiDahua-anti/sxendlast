using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Message.Entitys;
using JNPF.Message.Entitys.Entity;
using JNPF.Systems.Entitys.Dto.System.MenuData;
using JNPF.Systems.Entitys.Dto.System.System;
using JNPF.Systems.Entitys.Model.HomeData;
using JNPF.Systems.Entitys.Model.System.HomeData;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Interfaces.Repository;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 数据集.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "HomeData", Order = 217)]
[Route("api/system/[controller]")]
public class HomeDataService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<SystemEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 流程相关.
    /// </summary>
    private readonly IWorkFlowRepository _flowTaskRepository;

    /// <summary>
    /// 功能模块.
    /// </summary>
    private readonly IModuleService _moduleService;

    /// <summary>
    /// 初始化一个<see cref="HomeDataService"/>类型的新实例.
    /// </summary>
    public HomeDataService(
        ISqlSugarRepository<SystemEntity> repository,
        IUserManager userManager,
        IWorkFlowRepository flowTaskRepository,
        IModuleService moduleService)
    {
        _repository = repository;
        _flowTaskRepository = flowTaskRepository;
        _userManager = userManager;
        _moduleService = moduleService;
    }

    private readonly string _no1_welcome = "welcome";
    private readonly string _no2_banner = "banner";
    private readonly string _no3_workFlow = "workFlow";
    private readonly string _no4_teamwork = "teamwork";
    private readonly string _no5_notice = "notice";
    private readonly string _no6_message = "message";
    private readonly string _no7_latelyUse = "latelyUse";
    private readonly string _no8_commonUse = "commonUse";
    private readonly string _no9_favorites = "favorites";
    private readonly string _no10_system = "system";
    private readonly string _no11_pieChart = "pieChart";
    private readonly string _no12_columnChart = "columnChart";
    private readonly string _no13_server = "server";
    private readonly string _no14_help = "help";

    #region Get

    /// <summary>
    /// 获取主页.
    /// </summary>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetHomepage()
    {
        var list = new List<HomepageModel>();
        var allSystemList = await _repository.AsQueryable().OrderByDescending(x => x.CreatorTime).ToListAsync();
        var systemList = allSystemList.FindAll(x => x.DeleteMark == null);
        var systemCode = _userManager.AppCode.IsNotEmptyOrNull() ? _userManager.AppCode : CommonConst.SystemCodeMain;
        var moduleUseNumList = await _repository.AsSugarClient().Queryable<ModuleUseNumEntity>().Where(x => x.UserId.Equals(_userManager.UserId)).ToListAsync();
        var moduleList = await _moduleService.GetUserAllModuleList("Web");
        var flowEnabled = moduleList.Any(x => CommonConst.ModuleCodeFlow.Split(",").Contains(x.EnCode));
        var teamEnabled = moduleList.Any(x => CommonConst.ModuleCodeTeam.Split(",").Contains(x.EnCode));

        // 1-欢迎语
        list.Add(new HomepageModel() { code = _no1_welcome, enable = 1 });

        // 2-banner
        if (_userManager.IsManageRole.Value || _userManager.IsDevRole.Value || _userManager.IsAdministrator)
            list.Add(new HomepageModel() { code = _no2_banner, enable = 1 });
        else
            list.Add(new HomepageModel() { code = _no2_banner });

        // 3-流程数据
        var workFlowSystemId = systemList.Find(x => x.EnCode == _no3_workFlow)?.Id;
        var workFlowModelList = moduleList.FindAll(x => x.SystemId == workFlowSystemId);
        if (workFlowModelList.Count > 0)
        {
            var workFlowModel = GetApprovalCenter(systemList, workFlowModelList);
            list.Add(workFlowModel);
        }
        else
        {
            list.Add(new HomepageModel() { code = _no3_workFlow });
        }

        // 4-协作数据
        if (teamEnabled)
        {
            var teamworkModel = GetTeamwork(systemList, moduleList);
            list.Add(teamworkModel);
        }
        else
        {
            list.Add(new HomepageModel() { code = _no4_teamwork });
        }

        // 5-公告
        var noticeModel = GetNotice();
        list.Add(noticeModel);

        // 6-消息
        var messageModel = GetMessage();
        list.Add(messageModel);

        // 7-最近使用
        var latelyUseModule = GetCommonUseModule(moduleUseNumList, moduleList, 0);
        var latelyUseModel = new HomepageModel() { code = _no7_latelyUse, enable = 1, appList = latelyUseModule, flowEnabled = flowEnabled };
        if (flowEnabled)
        {
            latelyUseModel.flowList = _repository.AsSugarClient().Queryable<WorkFlowTemplateUseNumEntity, WorkFlowTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id))
                 .Where((a, b) => a.CreatorUserId == _userManager.UserId).OrderByDescending((a, b) => a.LastModifyTime)
                 .Select((a, b) => new HomeFlowListModel
                 {
                     id = b.Id,
                     fullName = b.FullName,
                     icon = b.Icon,
                     iconBackground = b.IconBackground,
                     type = b.Type
                 }).ToList().Take(12);
        }

        list.Add(latelyUseModel);

        // 8-最近常用
        var commonUseModule = GetCommonUseModule(moduleUseNumList, moduleList, 1);
        var commonUseModel = new HomepageModel() { code = _no8_commonUse, enable = 1, appList = commonUseModule, flowEnabled = flowEnabled };
        if (flowEnabled)
        {
            commonUseModel.flowList = _repository.AsSugarClient().Queryable<WorkFlowTemplateUseNumEntity, WorkFlowTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id))
                 .Where((a, b) => a.CreatorUserId == _userManager.UserId).OrderBy((a, b) => a.UseNum)
                 .Select((a, b) => new HomeFlowListModel
                 {
                     id = b.Id,
                     fullName = b.FullName,
                     icon = b.Icon,
                     iconBackground = b.IconBackground,
                     type = b.Type,
                     useNum = SqlFunc.ToString(a.UseNum)
                 }).ToList().Take(12);
        }

        list.Add(commonUseModel);

        // 9-我的收藏
        var favoritesMenuList = await GetFavoritesMenu(moduleList, systemList);
        var favoritesMenuModel = new HomepageModel() { code = _no9_favorites, enable = 1, appList = favoritesMenuList, flowEnabled = flowEnabled };
        if (flowEnabled)
        {
            favoritesMenuModel.flowList = _repository.AsSugarClient().Queryable<WorkFlowCommonEntity, WorkFlowTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.FlowId == b.Id))
                 .Where((a, b) => a.CreatorUserId == _userManager.UserId).OrderByDescending((a, b) => a.CreatorTime)
                 .Select((a, b) => new HomeFlowListModel
                 {
                     id = b.Id,
                     fullName = b.FullName,
                     icon = b.Icon,
                     iconBackground = b.IconBackground,
                     type = b.Type,
                     visibleType = b.VisibleType,
                     isCommonFlow = true
                 }).ToList().Take(12);
        }

        list.Add(favoritesMenuModel);

        // 10-我的应用
        var sysList = systemList.FindAll(x => !x.IsMain.Equals(1)).ToList().Adapt<List<SystemCrInput>>();
        if (!_userManager.IsAdministrator)
        {
            var authorIds = await _userManager.GetUserAuthorizeItemIdList(CommonConst.AuthorizeItemSystem);
            sysList = sysList.FindAll(x => authorIds.Contains(x.id));
        }

        list.Add(new HomepageModel() { code = _no10_system, enable = 1, data = sysList });

        // 11-饼图、12-柱形图、13-服务器信息
        if (_userManager.IsManageRole.Value || _userManager.IsAdministrator)
        {
            var roleList = await _repository.AsSugarClient().Queryable<RoleEntity>()
                .Where(x => x.DeleteMark == null && x.Type.Equals(CommonConst.RelationObjectUser))
                .Select(x => new PieChartModel
                {
                    name = x.FullName,
                    data = SqlFunc.Subqueryable<RoleRelationEntity>().EnableTableFilter().Where(it => it.RoleId == x.Id).Count()
                })
                .ToListAsync();
            list.Add(new HomepageModel() { code = _no11_pieChart, enable = 1, data = roleList });

            list.Add(GetColumnChartModel(allSystemList));

            list.Add(new HomepageModel() { code = _no13_server, enable = 1 });
        }
        else
        {
            list.Add(new HomepageModel() { code = _no11_pieChart });
            list.Add(new HomepageModel() { code = _no12_columnChart });
            list.Add(new HomepageModel() { code = _no13_server });
        }

        // 14-帮助
        var helpJson = await _repository.AsSugarClient().Queryable<SysConfigEntity>().Where(x => x.Key.Equals("help")).Select(x => x.Value).FirstAsync();
        if (helpJson.IsNotEmptyOrNull())
        {
            var help = helpJson.ToObject<Dictionary<string, object>>()["net"];
            list.Add(new HomepageModel() { code = _no14_help, enable = 1, data = help });
        }
        else
        {
            list.Add(new HomepageModel() { code = _no14_help });
        }

        return list;
    }

    /// <summary>
    /// 获取主页.
    /// </summary>
    /// <returns></returns>
    [HttpGet("app")]
    public async Task<dynamic> GetAppHomepage()
    {
        var list = new List<HomepageModel>();
        var systemList = await _repository.AsQueryable().Where(x => x.DeleteMark == null).OrderByDescending(x => x.CreatorTime).ToListAsync();
        var moduleUseNumList = await _repository.AsSugarClient().Queryable<ModuleUseNumEntity>().Where(x => x.UserId.Equals(_userManager.UserId) && x.SystemCode.Equals(_userManager.AppCode)).ToListAsync();
        var moduleList = await _moduleService.GetUserAllModuleList("App");
        var flowEnabled = moduleList.Any(x => CommonConst.ModuleCodeFlow.Split(",").Contains(x.EnCode));

        // 7-最近使用
        var latelyUseModule = GetCommonUseModule(moduleUseNumList, moduleList, 0);
        var latelyUseModel = new HomepageModel() { code = _no7_latelyUse, enable = 1, appList = latelyUseModule, flowEnabled = flowEnabled };
        if (flowEnabled)
        {
            latelyUseModel.flowList = _repository.AsSugarClient().Queryable<WorkFlowTemplateUseNumEntity, WorkFlowTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id))
                 .Where((a, b) => a.CreatorUserId == _userManager.UserId && b.SystemId == _userManager.SystemId).OrderByDescending((a, b) => a.LastModifyTime)
                 .Select((a, b) => new HomeFlowListModel
                 {
                     id = b.Id,
                     fullName = b.FullName,
                     icon = b.Icon,
                     iconBackground = b.IconBackground,
                     type = b.Type
                 }).ToList().Take(12);
        }

        list.Add(latelyUseModel);

        // 8-最近常用
        var commonUseModule = GetCommonUseModule(moduleUseNumList, moduleList, 1);
        var commonUseModel = new HomepageModel() { code = _no8_commonUse, enable = 1, appList = commonUseModule, flowEnabled = flowEnabled };
        if (flowEnabled)
        {
            commonUseModel.flowList = _repository.AsSugarClient().Queryable<WorkFlowTemplateUseNumEntity, WorkFlowTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id))
                 .Where((a, b) => a.CreatorUserId == _userManager.UserId && b.SystemId == _userManager.SystemId).OrderBy((a, b) => a.UseNum)
                 .Select((a, b) => new HomeFlowListModel
                 {
                     id = b.Id,
                     fullName = b.FullName,
                     icon = b.Icon,
                     iconBackground = b.IconBackground,
                     type = b.Type,
                     useNum = SqlFunc.ToString(a.UseNum)
                 }).ToList().Take(12);
        }

        list.Add(commonUseModel);

        // 9-我的收藏
        var favoritesMenuList = await GetFavoritesMenu(moduleList, systemList);
        var favoritesMenuModel = new HomepageModel() { code = _no9_favorites, enable = 1, appList = favoritesMenuList, flowEnabled = flowEnabled };
        if (flowEnabled)
        {
            favoritesMenuModel.flowList = _repository.AsSugarClient().Queryable<WorkFlowCommonEntity, WorkFlowTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.FlowId == b.Id))
                 .Where((a, b) => a.CreatorUserId == _userManager.UserId && b.SystemId == _userManager.SystemId).OrderByDescending((a, b) => a.CreatorTime)
                 .Select((a, b) => new HomeFlowListModel
                 {
                     id = b.Id,
                     fullName = b.FullName,
                     icon = b.Icon,
                     iconBackground = b.IconBackground,
                     type = b.Type,
                     visibleType = b.VisibleType,
                     isCommonFlow = true
                 }).ToList().Take(12);
        }

        list.Add(favoritesMenuModel);

        return list;
    }

    #endregion

    #region Private

    /// <summary>
    /// 获取常用菜单.
    /// </summary>
    /// <param name="moduleUseNumList"></param>
    /// <param name="moduleList"></param>
    /// <param name="systemList"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private List<ModuleDataListOutput> GetCommonUseModule(List<ModuleUseNumEntity> moduleUseNumList, List<ModuleEntity> moduleList, int type)
    {
        switch (type)
        {
            case 0:
                moduleUseNumList = moduleUseNumList.OrderByDescending(x => x.LastModifyTime).ToList();
                break;
            case 1:
                moduleUseNumList = moduleUseNumList.OrderByDescending(x => x.UseNum).ToList();
                break;
        }

        var useModuleList = new List<ModuleDataListOutput>();
        var moduleCodeBackEnd = CommonConst.ModuleCodeBackend.Split(",").ToList();
        foreach (var moduleUseNum in moduleUseNumList)
        {
            var module = moduleList.Find(x => x.Id.Equals(moduleUseNum.ModuleId));
            if (module.IsNotEmptyOrNull())
            {
                var newModule = module.Adapt<ModuleDataListOutput>();
                newModule.systemCode = moduleUseNum.SystemCode;
                if (moduleCodeBackEnd.Contains(newModule.enCode)) newModule.isBackend = 1;
                useModuleList.Add(newModule);
                if (useModuleList.Count == 12) break;
            }
        }

        return useModuleList;
    }

    /// <summary>
    /// 获取收藏菜单.
    /// </summary>
    /// <param name="moduleList"></param>
    /// <param name="systemList"></param>
    /// <returns></returns>
    private async Task<List<ModuleDataListOutput>> GetFavoritesMenu(List<ModuleEntity> moduleList, List<SystemEntity> systemList)
    {
        var favoritesMenuList = new List<ModuleDataListOutput>();
        var moduleType = _userManager.UserOrigin == "pc" ? "Web" : "App";
        var moduleIds = await _repository.AsSugarClient().Queryable<ModuleDataEntity>().Where(x => x.ModuleType == moduleType && x.CreatorUserId == _userManager.UserId && x.DeleteMark == null).OrderBy(x => x.CreatorTime).Select(x => x.ModuleId).ToListAsync();
        foreach (var moduleId in moduleIds)
        {
            var module = moduleList.Find(x => x.Id.Equals(moduleId));
            if (module.IsNotEmptyOrNull())
            {
                var newModule = module.Adapt<ModuleDataListOutput>();
                newModule.systemCode = systemList.Find(x => x.Id.Equals(newModule.systemId))?.EnCode;
                favoritesMenuList.Add(newModule);
            }
        }

        return favoritesMenuList;
    }

    /// <summary>
    /// 获取柱形图.
    /// </summary>
    /// <param name="systemList"></param>
    /// <returns></returns>
    private HomepageModel GetColumnChartModel(List<SystemEntity> systemList)
    {
        var res = new HomepageModel() { code = _no12_columnChart, enable = 1 };

        var dateList = Enumerable.Range(0, 31)
            .Select(offset => DateTime.Today.AddDays(-offset))
            .Select(date => date.ToString("yyyy-MM-dd"))
            .ToList();
        dateList.Reverse();

        var category = new List<string>();
        var createData = new List<int>();
        var updateData = new List<int>();
        foreach (var date in dateList)
        {
            category.Add(date.Substring(5));
            var dateTime = date.ParseToDateTime();
            createData.Add(systemList.FindAll(x => x.CreatorTime.IsNotEmptyOrNull() && x.CreatorTime.Value.Date.Equals(dateTime.Date)).Count());
            updateData.Add(systemList.FindAll(x => x.LastModifyTime.IsNotEmptyOrNull() && x.LastModifyTime.Value.Date.Equals(dateTime.Date)).Count());
        }

        res.data = new List<ColumnChartModel>
        {
            new ColumnChartModel()
            {
                name = "创建",
                data = createData,
                category = category
            },
            new ColumnChartModel()
            {
                name = "修改",
                data = updateData,
                category = category
            }
        };

        return res;
    }

    /// <summary>
    /// 审批中心.
    /// </summary>
    /// <returns></returns>
    private HomepageModel GetApprovalCenter(List<SystemEntity> systemList, List<ModuleEntity> moduleList)
    {
        var model = new HomepageModel() { code = _no3_workFlow, enable = 1 };
        var modelList = moduleList.FindAll(x => x.Type == 2).Adapt<List<HomeDataModel>>();
        foreach (var item in modelList)
        {
            switch (item.enCode)
            {
                case "workFlow.flowLaunch":
                    item.count = _flowTaskRepository.GetFlowTaskList(5).Count;
                    break;
                case "workFlow.flowToSign":
                    item.count = _flowTaskRepository.GetFlowTaskList(0).Count;
                    break;
                case "workFlow.flowTodo":
                    item.count = _flowTaskRepository.GetFlowTaskList(1).Count;
                    break;
                case "workFlow.flowDoing":
                    item.count = _flowTaskRepository.GetFlowTaskList(2).Count;
                    break;
                case "workFlow.flowDone":
                    item.count = _flowTaskRepository.GetFlowTaskList(3).Count;
                    break;
                case "workFlow.flowCirculate":
                    item.count = _flowTaskRepository.GetFlowTaskList(4).Count;
                    break;
            }
        }

        model.data = modelList;
        return model;
    }

    /// <summary>
    /// 协同办公.
    /// </summary>
    /// <param name="systemList"></param>
    /// <param name="moduleList"></param>
    /// <returns></returns>
    private HomepageModel GetTeamwork(List<SystemEntity> systemList, List<ModuleEntity> moduleList)
    {
        var model = new HomepageModel() { code = _no4_teamwork, enable = 1 };
        var systemId = systemList.Find(x => x.EnCode == _no4_teamwork)?.Id;
        var modelList = moduleList.FindAll(x => x.SystemId == systemId && x.Type == 2).Adapt<List<HomeDataModel>>();
        model.data = modelList;
        return model;
    }

    public HomepageModel GetNotice()
    {
        var output = new List<HomeDataNoticeItem>();
        List<HomeDataNoticeItem> list = _repository.AsSugarClient().Queryable<MessageEntity>()
            .Where(a => a.Type == 1 && a.DeleteMark == null && a.UserId == _userManager.UserId)
            .OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select((a) => new HomeDataNoticeItem()
            {
                id = a.Id,
                fullName = a.Title,
                creatorTime = a.CreatorTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                excerpt = a.BodyText
            }).ToList();
        foreach (var item in list)
        {
            if (item.excerpt.IsNotEmptyOrNull())
            {
                var noticeDic = item.excerpt.ToObject<Dictionary<string, object>>();
                var noticeId = string.Empty;
                if (noticeDic.ContainsKey("id"))
                {
                    noticeId = noticeDic["id"].ToString();
                }
                if (noticeDic.ContainsKey("Id"))
                {
                    noticeId = noticeDic["Id"].ToString();
                }
                var noticeEntity = _repository.AsSugarClient().Queryable<NoticeEntity>().First(x => x.DeleteMark == null && x.Id == noticeId);
                if (noticeEntity.IsNotEmptyOrNull())
                {
                    item.category = noticeEntity.Category.Equals("1") ? "公告" : "通知";
                    item.coverImage = noticeEntity.CoverImage;
                    item.excerpt = noticeEntity.Description;
                    item.releaseTime = noticeEntity.LastModifyTime;
                    item.releaseUser = _userManager.GetUserName(noticeEntity.LastModifyUserId);
                    output.Add(item);
                }
                if (output.Count == 5)
                    break;
            }
        }

        return new HomepageModel() { code = _no5_notice, enable = 1, data = output };
    }

    public HomepageModel GetMessage()
    {
        var msgSourceType = _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().First(x => x.EnCode == "msgSourceType" && x.DeleteMark == null);
        var list = _repository.AsSugarClient().Queryable<MessageEntity>()
            .Where(a => a.UserId == _userManager.UserId && a.DeleteMark == null)
            .OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select(a => new HomeDataMessageItem
            {
                id = a.Id,
                releaseTime = a.CreatorTime,
                releaseUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                title = a.Title,
                type = a.Type,
                typeName = SqlFunc.Subqueryable<DictionaryDataEntity>().EnableTableFilter().Where(u => u.DictionaryTypeId == msgSourceType.Id && SqlFunc.ToInt32(u.EnCode) == a.Type).Select(u => u.FullName),
                isRead = a.IsRead,
                flowType = a.FlowType
            }).ToList().Skip(5);
        return new HomepageModel() { code = _no6_message, enable = 1, data = list };
    }

    #endregion
}
