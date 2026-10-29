using JNPF.Common.CodeGen.DataParsing;
using JNPF.Common.Core.Manager;
using JNPF.Common.Dtos;
using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Models;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.System;
using JNPF.example.Entitys.Dto.SxPurchaseArrivalMain;
using JNPF.example.Entitys.Dto.SxPurchaseArrivalDetail;
using JNPF.example.Entitys;
using JNPF.example.Interfaces;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.Systems.Interfaces.Custom;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.example;

/// <summary>
/// 业务实现：sx_purchase_arrival_main（到货单主表）.
/// 对应前端页面：sxPurchaseArrivalMain（index.vue / Form.vue / Detail.vue）.
/// 前端 API 封装：helper/api.ts.
/// </summary>
/// <remarks>
/// <para>【API 接口清单】</para>
/// <list type="bullet">
///   <item><term>GET  {id}                     </term><description>→ 编辑回显（getInfo）</description></item>
///   <item><term>POST List                     </term><description>→ 列表分页 + 高级查询（getList）含流程卡打印状态</description></item>
///   <item><term>POST /                        </term><description>→ 新增保存（create）</description></item>
///   <item><term>PUT  {id}                     </term><description>→ 编辑保存（update）</description></item>
///   <item><term>DELETE {id}                   </term><description>→ 单条删除（del）</description></item>
///   <item><term>POST batchRemove              </term><description>→ 批量删除（batchDelete）</description></item>
///   <item><term>GET  Detail/{id}              </term><description>→ 详情弹窗（getDetail）</description></item>
///   <item><term>POST PrintFlowCardForDetail   </term><description>→ 打印流程卡（回写detail投产数量/总托数/打印次数）</description></item>
///   <item><term>POST DeleteFlowCard           </term><description>→ 删除一行流程卡（软删，已投产拦截）</description></item>
///   <item><term>GET  GetArrivalPrintContext   </term><description>→ 打印弹窗上下文（从 detail 表取字段）</description></item>
///   <item><term>POST SyncFromYonyou           </term><description>→ 用友同步到货单（模拟）</description></item>
///   <item><term>POST PreviewAllocate          </term><description>→ 打印预览分配（按总托数平均分配，自动生成流程卡ID）</description></item>
///   <item><term>POST GenerateFlowCard         </term><description>→ 新增一行流程卡（生成唯一卡号，考虑历史）</description></item>
/// </list>
/// </remarks>
[ApiDescriptionSettings(Tag = "SxPurchaseArrivalMain", Name = "", Order = 200)]
[Route("api/[controller]")]
public class SxPurchaseArrivalMainService : ISxPurchaseArrivalMainService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 到货单主表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<SxPurchaseArrivalMainEntity> _repository;

    /// <summary>
    /// 数据库管理.
    /// </summary>
    private readonly IDataBaseManager _dataBaseManager;

    /// <summary>
    /// 通用数据解析（高级查询）.
    /// </summary>
    private readonly ControlParsing _controlParsing;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 流程卡仓储（主库）.
    /// </summary>
    private readonly ISqlSugarRepository<FlowCardEntity> _flowCardRepository;

    /// <summary>
    /// 生产订单仓储（工作令号匹配）.
    /// </summary>
    private readonly ISqlSugarRepository<ProductionOrderEntity> _productionOrderRepository;

    /// <summary>
    /// 用友同步服务.
    /// </summary>
    private readonly IYonyouSyncService _yonyouSyncService;

    /// <summary>
    /// 外部库客户端（到货单所在库）.
    /// </summary>
    private static SqlSugarScope? _sqlSugarClient;

    /// <summary>
    /// 初始化一个<see cref="SxPurchaseArrivalMainService"/>类型的新实例.
    /// </summary>
    public SxPurchaseArrivalMainService(
        ISqlSugarRepository<SxPurchaseArrivalMainEntity> repository,
        IDataBaseManager dataBaseManager,
        ISqlSugarClient context,
        ControlParsing controlParsing,
        IUserManager userManager,
        ISqlSugarRepository<FlowCardEntity> flowCardRepository,
        ISqlSugarRepository<ProductionOrderEntity> productionOrderRepository,
        IYonyouSyncService yonyouSyncService)
    {
        _repository = repository;
        _dataBaseManager = dataBaseManager;
        _sqlSugarClient = (SqlSugarScope)context;
        _controlParsing = controlParsing;
        _userManager = userManager;
        _flowCardRepository = flowCardRepository;
        _productionOrderRepository = productionOrderRepository;
        _yonyouSyncService = yonyouSyncService;
    }

    // ──────────────────────────────────────────────────────────
    // 前端对照：Form.vue → getInfo(id)  编辑回显
    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// 获取sx_purchase_arrival_main（编辑回显）.
    /// 前端调用：Form.vue → getData(id) → getInfo(id) → GET /{id}.
    /// 返回主表 + 子表（SxPurchaseArrivalDetailList）.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var dbLink = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("1980101230294339584"));
        _sqlSugarClient = _dataBaseManager.ChangeDataBase(dbLink);

        var data = (await _sqlSugarClient.Queryable<SxPurchaseArrivalMainEntity>()
            .Includes(it => it.SxPurchaseArrivalDetailList)
            .Select(it => new SxPurchaseArrivalMainEntity
            {
                Id = it.Id,
                Business_type = it.Business_type,
                Purchase_type = it.Purchase_type,
                Arrival_date = it.Arrival_date,
                Bill_no = it.Bill_no,
                Supplier_name = it.Supplier_name,
                Purchase_remark = it.Purchase_remark,
                SxPurchaseArrivalDetailList = it.SxPurchaseArrivalDetailList,
            })
            .FirstAsync(it => it.Id.Equals(id))).Adapt<SxPurchaseArrivalMainInfoOutput>(); 

        if (data == null) return data;
        return data;
    }

    // ──────────────────────────────────────────────────────────
    // 前端对照：index.vue → getList(data)  列表分页 + 高级查询
    // 搜索条件：business_type（模糊）、arrival_date（日期范围）
    // 高级查询：superQueryJson（主表 + 子表动态条件）
    // 过滤条件：FlowId==null（排除流程引擎关联数据）
    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// 获取sx_purchase_arrival_main列表（分页 + 高级查询）.
    /// 前端调用：index.vue → useVxeTable({ api: getList }) → POST /List.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("List")]
    public async Task<dynamic> GetList([FromBody] SxPurchaseArrivalMainListQueryInput input)
    {
        var dbLink = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("1980101230294339584"));
        _sqlSugarClient = _dataBaseManager.ChangeDataBase(dbLink);

        var entityInfo = _sqlSugarClient.EntityMaintenance.GetEntityInfo<SxPurchaseArrivalMainEntity>();
        var superQuery = SuperQueryHelper.GetSuperQueryInput(input.superQueryJson, string.Empty, entityInfo, 0);
        superQuery = await _controlParsing.ReplaceDynamicQuery<ConvertSuper>(superQuery);
        List<IConditionalModel> mainConditionalModel = SuperQueryHelper.GetSuperQueryJson(superQuery);

        entityInfo = _sqlSugarClient.EntityMaintenance.GetEntityInfo<SxPurchaseArrivalDetailEntity>();
        superQuery = SuperQueryHelper.GetSuperQueryInput(input.superQueryJson, "tableField7c2932-", entityInfo, 1);
        superQuery = await _controlParsing.ReplaceDynamicQuery<ConvertSuper>(superQuery);
        List<IConditionalModel> sxPurchaseArrivalDetailConditionalModel = SuperQueryHelper.GetSuperQueryJson(superQuery);

        var selectIds = input.selectIds?.Split(",").ToList();
        var data = await _sqlSugarClient.Queryable<SxPurchaseArrivalMainEntity>()
            .WhereIF(selectIds!=null && selectIds.Any() && input.dataType.Equals(2), it => selectIds.Contains(it.Id))
            .WhereIF(!string.IsNullOrEmpty(input.business_type), it => it.Business_type.Contains(input.business_type))
            .WhereIF(input.arrival_date?.Count() > 0, it => SqlFunc.Between(it.Arrival_date, input.arrival_date.First().ParseToDateTime("yyyy-MM-dd HH:mm:ss"), input.arrival_date.Last().ParseToDateTime("yyyy-MM-dd HH:mm:ss")))
            .Where(mainConditionalModel)
            .WhereIF(sxPurchaseArrivalDetailConditionalModel.Count > 0, it => it.SxPurchaseArrivalDetailList.Any(sxPurchaseArrivalDetailConditionalModel))
            .Where(it => it.FlowId==null)
            .OrderByIF(string.IsNullOrEmpty(input.sidx), it => it.Id).OrderByIF(!string.IsNullOrEmpty(input.sidx), input.sidx + " " + input.sort)
            .Select(it => new SxPurchaseArrivalMainListOutput
            {
                id = it.Id,
                business_type = it.Business_type,
                purchase_type = it.Purchase_type,
                arrival_date = it.Arrival_date.ToString(),
                bill_no = it.Bill_no,
                supplier_name = it.Supplier_name,
                purchase_remark = it.Purchase_remark,
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);

        await _repository.AsSugarClient().ThenMapperAsync(data.list, async item =>
        {
            var linkageParameters = new List<DataInterfaceParameter>();
        });

        // 计算每行流程卡打印状态
        var arrivalIds = data.list.Select(m => m.id).Where(m => !m.IsNullOrEmpty()).ToList();
        if (arrivalIds.Any())
        {
            var allDetails = await _sqlSugarClient.Queryable<SxPurchaseArrivalDetailEntity>()
                .Where(m => arrivalIds.Contains(m.Arrival_main_id))
                .ToListAsync();
            var detailIds = allDetails.Select(m => m.Id).ToList();
            var allCards = detailIds.Any()
                ? await _flowCardRepository.AsQueryable()
                    .Where(m => m.ArrivalDetailId != null && detailIds.Contains(m.ArrivalDetailId)
                        && m.DeleteMark == null && (m.Status ?? 1) != 5)
                    .ToListAsync()
                : new List<FlowCardEntity>();

            foreach (var item in data.list)
            {
                var details = allDetails.Where(m => m.Arrival_main_id == item.id).ToList();
                var printed = details.Count(d =>
                    allCards.Any(c => c.ArrivalDetailId == d.Id));
                var total = details.Count;
                var status = total == 0 ? 0 : printed == 0 ? 0 : printed == total ? 2 : 1;
                item.flow_card_print_status = status;
                item.flow_card_print_status_text = status == 0 ? "未打印" : status == 2 ? "已打印" : "部分打印";
                item.flow_card_printed_text = $"{printed}/{total}";
            }
        }

        return PageResult<SxPurchaseArrivalMainListOutput>.SqlSugarPageResult(data);
    }

    // ──────────────────────────────────────────────────────────
    // 前端对照：Form.vue → handleSubmit() → create(data)  新增保存
    // 主表 + 子表（tableField7c2932）一起提交
    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// 新建sx_purchase_arrival_main（含子表到货列表）.
    /// 前端调用：Form.vue → handleSubmit() → create(data) → POST /.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] SxPurchaseArrivalMainCrInput input)
    {
        var dbLink = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("1980101230294339584"));
        _sqlSugarClient = _dataBaseManager.ChangeDataBase(dbLink);

        var entity = input.Adapt<SxPurchaseArrivalMainEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        await _sqlSugarClient.Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

        if (input.tableField7c2932 != null)
        {
            foreach (var item in input.tableField7c2932)
            {
                var detail = item.Adapt<SxPurchaseArrivalDetailEntity>();
                detail.Id = SnowflakeIdHelper.NextId();
                detail.Arrival_main_id = entity.Id;
                await _sqlSugarClient.Insertable(detail).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    // 前端对照：Form.vue → handleSubmit() → update(data)  编辑保存
    // 逻辑：先删子表已移除行 → 再更新主表 → 逐行新增/更新子表
    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// 更新sx_purchase_arrival_main（含子表到货列表增删改）.
    /// 前端调用：Form.vue → handleSubmit() → update(data) → PUT /{id}.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] SxPurchaseArrivalMainUpInput input)
    {
        var dbLink = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("1980101230294339584"));
        _sqlSugarClient = _dataBaseManager.ChangeDataBase(dbLink);

        var existing = await _sqlSugarClient.Queryable<SxPurchaseArrivalMainEntity>().FirstAsync(m => m.Id == id);
        if (existing == null) throw Oops.Oh(ErrorCode.COM1005);

        // 更新主表
        input.Adapt(existing);
        await _sqlSugarClient.Updateable(existing)
            .IgnoreColumns(ignoreAllNullColumns: true)
            .CallEntityMethod(m => m.LastModify())
            .ExecuteCommandAsync();

        // 子表：删除前端已移除的行
        var keepIds = input.tableField7c2932?.Where(d => !d.F_ID.IsNullOrEmpty()).Select(d => d.F_ID).ToList() ?? new List<string>();
        if (keepIds.Any())
            await _sqlSugarClient.Deleteable<SxPurchaseArrivalDetailEntity>().Where(d => d.Arrival_main_id == id && !keepIds.Contains(d.Id)).ExecuteCommandAsync();
        else
            await _sqlSugarClient.Deleteable<SxPurchaseArrivalDetailEntity>().Where(d => d.Arrival_main_id == id).ExecuteCommandAsync();

        // 子表：新增/更新
        if (input.tableField7c2932 != null)
        {
            foreach (var item in input.tableField7c2932)
            {
                var detail = item.Adapt<SxPurchaseArrivalDetailEntity>();
                if (item.F_ID.IsNullOrEmpty())
                {
                    detail.Id = SnowflakeIdHelper.NextId();
                    detail.Arrival_main_id = id;
                    await _sqlSugarClient.Insertable(detail).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }
                else
                {
                    await _sqlSugarClient.Updateable(detail).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    // 前端对照：index.vue → del(id)  单条删除
    // 同时级联删除子表数据
    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// 删除sx_purchase_arrival_main（级联删除子表）.
    /// 前端调用：index.vue → del(id) → DELETE /{id}.
    /// </summary>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var dbLink = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("1980101230294339584"));
        _sqlSugarClient = _dataBaseManager.ChangeDataBase(dbLink);

        var entity = await _sqlSugarClient.Queryable<SxPurchaseArrivalMainEntity>().FirstAsync(m => m.Id == id);
        if (entity == null) throw Oops.Oh(ErrorCode.COM1005);

        await _sqlSugarClient.Deleteable<SxPurchaseArrivalMainEntity>().Where(m => m.Id == id).ExecuteCommandAsync();
        await _sqlSugarClient.Deleteable<SxPurchaseArrivalDetailEntity>().Where(m => m.Arrival_main_id == id).ExecuteCommandAsync();
    }

    // ──────────────────────────────────────────────────────────
    // 前端对照：index.vue → batchDelete(ids)  批量删除
    // 同时级联删除所有选中记录的子表数据
    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// 批量删除sx_purchase_arrival_main（级联删除子表）.
    /// 前端调用：index.vue → batchDelete(ids) → POST /batchRemove.
    /// </summary>
    /// <param name="input">主键数组.</param>
    /// <returns></returns>
    [HttpPost("batchRemove")]
    public async Task BatchRemove([FromBody] BatchRemoveInput input)
    {
        var dbLink = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("1980101230294339584"));
        _sqlSugarClient = _dataBaseManager.ChangeDataBase(dbLink);

        var ids = input.ids.ToList();
        await _sqlSugarClient.Deleteable<SxPurchaseArrivalDetailEntity>().Where(m => ids.Contains(m.Arrival_main_id)).ExecuteCommandAsync();
        await _sqlSugarClient.Deleteable<SxPurchaseArrivalMainEntity>().Where(m => ids.Contains(m.Id)).ExecuteCommandAsync();
    }

    // ──────────────────────────────────────────────────────────
    // 前端对照：Detail.vue → getDetail(id)  详情弹窗
    // 返回主表 + 子表（tableField7c2932）
    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// sx_purchase_arrival_main详情（只读查看）.
    /// 前端调用：Detail.vue → getData(id) → getDetail(id) → GET /Detail/{id}.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("Detail/{id}")]
    [UnifySerializerSetting("special")]
    public async Task<dynamic> GetDetails(string id)
    {
        var dbLink = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("1980101230294339584"));
        _sqlSugarClient = _dataBaseManager.ChangeDataBase(dbLink);

        var data = await _sqlSugarClient.Queryable<SxPurchaseArrivalMainEntity>()
            .Includes(it => it.SxPurchaseArrivalDetailList)
            .Where(it => it.Id == id)
            .Select(it => new SxPurchaseArrivalMainDetailOutput
            {
                id = it.Id,
                business_type = it.Business_type,
                purchase_type = it.Purchase_type,
                arrival_date = it.Arrival_date.ToString(),
                bill_no = it.Bill_no,
                supplier_name = it.Supplier_name,
                purchase_remark = it.Purchase_remark,
                tableField7c2932 = it.SxPurchaseArrivalDetailList.Adapt<List<SxPurchaseArrivalDetailDetailOutput>>(),
            }).ToListAsync();



        return data.FirstOrDefault();
    }

    // ──────────────────────────────────────────────────────────
    // 前端对照：Detail.vue → openPrintModal(record) → handlePrint()
    // ⚠️ 当前前端使用 mock 数据，尚未调用此 API
    // 功能：到货明细打印流程卡（条数整定：不足补印、多余删除；
    //       已投产卡不可减少条数；支持修改定量）
    // ──────────────────────────────────────────────────────────
    #region 到货单打印流程卡

    // ──────────────────────────────────────────────────────────
    // 用友同步：到货单拉取（模拟实现）
    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// 从用友U8同步到货单（模拟实现：生成到货单并写同步日志）.
    /// </summary>
    /// <param name="input">拉取入参（可选 count 控制生成条数）.</param>
    /// <returns>同步结果.</returns>
    [HttpPost("SyncFromYonyou")]
    public async Task<dynamic> SyncFromYonyou([FromBody] SyncPullInput input)
    {
        var count = input?.Count ?? 2;
        if (count <= 0) count = 2;
        if (count > 20) count = 20;

        var dbLink = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("1980101230294339584"));
        var arrivalClient = _dataBaseManager.ChangeDataBase(dbLink);

        var now = DateTime.Now;
        var created = new List<SxPurchaseArrivalMainEntity>();
        for (var i = 1; i <= count; i++)
        {
            var billNo = "DH-" + now.ToString("yyyyMMdd") + "-" + (i + DateTime.Now.Hour).ToString("D3");
            var main = new SxPurchaseArrivalMainEntity
            {
                Id = SnowflakeIdHelper.NextId(),
                Business_type = "采购到货",
                Purchase_type = "原材料",
                Arrival_date = now.ToString("yyyy-MM-dd"),
                Bill_no = billNo,
                Supplier_name = $"用友同步供应商{i}",
                Purchase_remark = "用友U8同步（模拟）"
            };
            created.Add(main);
        }

        await arrivalClient.Insertable(created).ExecuteCommandAsync();

        // 写用友同步日志
        var bizNo = string.Join(",", created.Select(m => m.Bill_no));
        if (bizNo.Length > 50) bizNo = bizNo.Substring(0, 50);
        await _yonyouSyncService.WriteAutoPushLogAsync("到货单", bizNo, $"用友同步到货单 {count} 条");

        return new {
            success = true,
            imported = created.Count,
            billNos = created.Select(m => m.Bill_no).ToList()
        };
    }

    /// <summary>
    /// 打印预览
    /// </summary>
    /// <param name="arrivalDetailId">到货明细ID.</param>
    /// <returns>上下文.</returns>
    [HttpGet("GetArrivalPrintContext")]
    public async Task<dynamic> GetArrivalPrintContext(string arrivalDetailId)
    {
        if (arrivalDetailId.IsNullOrEmpty())
        {
            throw Oops.Oh(ErrorCode.COM1018, "到货明细ID不能为空");
        }

        var dbLink = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("1980101230294339584"));
        var arrivalClient = _dataBaseManager.ChangeDataBase(dbLink);
        var detail = await arrivalClient.Queryable<SxPurchaseArrivalDetailEntity>().FirstAsync(m => m.Id == arrivalDetailId);
        if (detail == null)
        {
            throw Oops.Oh(ErrorCode.COM1007);
        }

        var main = await arrivalClient.Queryable<SxPurchaseArrivalMainEntity>().FirstAsync(m => m.Id == detail.Arrival_main_id);

        var cards = await _flowCardRepository.AsQueryable()
            .Where(m => m.ArrivalDetailId == arrivalDetailId && m.DeleteMark == null && (m.Status ?? 1) != 5)
            .OrderBy(m => m.CreatorTime)
            .ToListAsync();

        return new ArrivalPrintContextOutput
        {
            ArrivalId = detail.Arrival_main_id,
            ArrivalDetailId = arrivalDetailId,
            DrawingNo = detail.Spec_model,
            WorkOrderNo = detail.Work_order_no,
            OrderNo = detail.Order_no,
            MaterialCode = detail.Inventory_code,
            MaterialName = detail.Inventory_name,
            Unit = detail.Main_unit,
            PlanDate = main?.Arrival_date.ToString(),
            MakeNo = detail.Make_no,
            TypeText = detail.Type_text,
            TotalQty = detail.Total_qty,
            TotalPallets = detail.Total_pallets,
            Cards = cards.Select(m => new ArrivalFlowCardItemOutput
            {
                CardId = m.Id,
                CardNo = m.CardNo,
                Qty = m.Qty,
            }).ToList()
        };
    }

    // ──────────────────────────────────────────────────────────
    // 打印预览分配：按总托数平均分配投产数量，自动生成流程卡并入库
    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// 打印预览分配：按总托数平均分配投产数量，自动生成流程卡并入库.
    /// POST /PreviewAllocate  入参：arrivalId、arrivalDetailId、totalQty、totalPallets.
    /// </summary>
    /// <param name="input">分配入参.</param>
    /// <returns>分配结果（含已入库的流程卡ID列表）.</returns>
    [HttpPost("PreviewAllocate")]
    public async Task<dynamic> PreviewAllocate([FromBody] ArrivalPreviewAllocateInput input)
    {
        if (input.ArrivalDetailId.IsNullOrEmpty())
        {
            throw Oops.Oh(ErrorCode.COM1018, "到货明细ID不能为空");
        }

        if (input.TotalQty <= 0)
        {
            throw Oops.Oh(ErrorCode.COM1018, "投产数量必须大于0");
        }

        if (input.TotalPallets <= 0)
        {
            throw Oops.Oh(ErrorCode.COM1018, "总托数必须大于0");
        }

        var dbLink = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("1980101230294339584"));
        var arrivalClient = _dataBaseManager.ChangeDataBase(dbLink);
        var detail = await arrivalClient.Queryable<SxPurchaseArrivalDetailEntity>().FirstAsync(m => m.Id == input.ArrivalDetailId);
        if (detail == null)
        {
            throw Oops.Oh(ErrorCode.COM1007);
        }

        // 平均分配：前 N-1 卡取整，最后一卡补齐余数
        var avgQty = Math.Floor(input.TotalQty / input.TotalPallets);
        var remainder = input.TotalQty - (avgQty * input.TotalPallets);

        var now = DateTime.Now;
        var printUser = _userManager.GetUserName(_userManager.UserId);
        var cards = new List<ArrivalPreviewAllocateCardItem>();
        var flowCards = new List<FlowCardEntity>();

        for (var i = 0; i < input.TotalPallets; i++)
        {
            var qty = (i == input.TotalPallets - 1) ? avgQty + remainder : avgQty;
            var cardNo = await GenerateNextFlowCardNoAsync();
            var cardId = SnowflakeIdHelper.NextId();

            var card = new FlowCardEntity
            {
                Id = cardId,
                CardNo = cardNo,
                CardType = 1,
                ArrivalId = input.ArrivalId,
                ArrivalDetailId = input.ArrivalDetailId,
                OrderNo = detail.Order_no ?? detail.Work_order_no,
                ProductName = detail.Inventory_name,
                DrawingNo = detail.Spec_model,
                Qty = qty,
                Unit = detail.Main_unit,
                PrintUser = printUser,
                PrintTime = now,
                Status = 1,
                Remark = $"打印预览分配生成：{detail.Inventory_code} 第{i + 1}/{input.TotalPallets}托"
            };
            flowCards.Add(card);

            cards.Add(new ArrivalPreviewAllocateCardItem
            {
                CardId = cardId,
                CardNo = cardNo,
                Qty = qty
            });
        }

        // 流程卡批量入库
        await _flowCardRepository.AsSugarClient().Insertable(flowCards)
            .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

        // 回写到货明细：投产数量、总托数、打印次数+1
        await arrivalClient.Updateable<SxPurchaseArrivalDetailEntity>()
            .SetColumns(m => new SxPurchaseArrivalDetailEntity
            {
                Total_qty = input.TotalQty,
                Total_pallets = input.TotalPallets,
                Print_count = (m.Print_count ?? 0) + 1
            })
            .Where(m => m.Id == input.ArrivalDetailId)
            .ExecuteCommandAsync();

        return new ArrivalPreviewAllocateOutput
        {
            ArrivalId = input.ArrivalId,
            ArrivalDetailId = input.ArrivalDetailId,
            TotalQty = input.TotalQty,
            TotalPallets = input.TotalPallets,
            QtyPerCard = avgQty,
            Cards = cards
        };
    }

    // ──────────────────────────────────────────────────────────
    // 新增一行流程卡：生成唯一流程卡ID（考虑历史卡号）
    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// 新增一行流程卡（打印预览页点击“新增一行”）.
    /// POST /GenerateFlowCard  入参：arrivalDetailId.
    /// 返回自动生成的唯一流程卡ID（考虑历史卡号）.
    /// </summary>
    /// <param name="input">入参.</param>
    /// <returns>新增流程卡ID.</returns>
    [HttpPost("GenerateFlowCard")]
    public async Task<dynamic> GenerateFlowCard([FromBody] ArrivalGenerateFlowCardInput input)
    {
        if (input.ArrivalId.IsNullOrEmpty() || input.ArrivalDetailId.IsNullOrEmpty())
        {
            throw Oops.Oh(ErrorCode.COM1018, "到货单与到货明细不能为空");
        }

        var dbLink = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("1980101230294339584"));
        var arrivalClient = _dataBaseManager.ChangeDataBase(dbLink);
        var detail = await arrivalClient.Queryable<SxPurchaseArrivalDetailEntity>().FirstAsync(m => m.Id == input.ArrivalDetailId);
        if (detail == null)
        {
            throw Oops.Oh(ErrorCode.COM1007);
        }

        var cardNo = await GenerateNextFlowCardNoAsync();
        var cardId = SnowflakeIdHelper.NextId();
        var now = DateTime.Now;

        var card = new FlowCardEntity
        {
            Id = cardId,
            CardNo = cardNo,
            CardType = 1,
            ArrivalId = input.ArrivalId,
            ArrivalDetailId = input.ArrivalDetailId,
            OrderNo = detail.Order_no ?? detail.Work_order_no,
            ProductName = detail.Inventory_name,
            DrawingNo = detail.Spec_model,
            Qty = 0,
            Unit = detail.Main_unit,
            PrintUser = _userManager.GetUserName(_userManager.UserId),
            PrintTime = now,
            Status = 1,
            Remark = $"到货单新增流程卡：{detail.Inventory_code}"
        };

        await _flowCardRepository.AsSugarClient().Insertable(card)
            .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

        return new ArrivalGenerateFlowCardOutput
        {
            CardId = cardId,
            CardNo = cardNo,
            Message = $"流程卡 {cardNo} 已生成"
        };
    }

    /// <summary>
    /// 生成下一个流程卡号（格式 LCK-yyyyMMdd-NNN，考虑历史卡号确保唯一）.
    /// </summary>
    private async Task<string> GenerateNextFlowCardNoAsync()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var prefix = $"LCK-{today}-";
        var maxCardNo = await _flowCardRepository.AsQueryable()
            .Where(m => m.CardNo != null && m.CardNo.StartsWith(prefix))
            .OrderByDescending(m => m.CardNo)
            .Select(m => m.CardNo)
            .FirstAsync();

        var seq = 1;
        if (maxCardNo != null)
        {
            var parts = maxCardNo.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[2], out var lastSeq))
            {
                seq = lastSeq + 1;
            }
        }

        var cardNo = $"{prefix}{seq:D3}";
        while (await _flowCardRepository.IsAnyAsync(m => m.CardNo == cardNo))
        {
            seq++;
            cardNo = $"{prefix}{seq:D3}";
        }

        return cardNo;
    }

    /// <summary>
    /// 删除一行流程卡（软删；已投产卡拦截）.
    /// </summary>
    [HttpPost("DeleteFlowCard")]
    public async Task<dynamic> DeleteFlowCard([FromBody] DeleteFlowCardInput input)
    {
        if (input.CardId.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.COM1018, "流程卡ID不能为空");

        var card = await _flowCardRepository.AsQueryable().FirstAsync(m => m.Id == input.CardId && m.DeleteMark == null);
        if (card == null) throw Oops.Oh(ErrorCode.COM1007);

        if ((card.Status ?? 1) > 1 && (card.Status ?? 1) != 5)
            throw Oops.Oh(ErrorCode.COM1018, $"流程卡 {card.CardNo} 已投产使用，不能删除");

        await _flowCardRepository.AsSugarClient().Updateable(card)
            .CallEntityMethod(m => m.Delete())
            .UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId })
            .ExecuteCommandAsync();

        return $"流程卡 {card.CardNo} 已删除";
    }

    #endregion






}
