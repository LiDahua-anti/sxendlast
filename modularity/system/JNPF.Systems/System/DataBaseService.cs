using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Dtos.DataBase;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.Database;
using JNPF.Systems.Entitys.Model.DataBase;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Collections;
using System.Data;
using System.Text;

namespace JNPF.Systems;

/// <summary>
/// 数据管理
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "DataModel", Order = 208)]
[Route("api/system/[controller]")]
public class DataBaseService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    public readonly ISqlSugarRepository<DbLinkEntity> _repository;

    /// <summary>
    /// 数据连接服务.
    /// </summary>
    private readonly IDbLinkService _dbLinkService;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 数据库管理.
    /// </summary>
    private readonly IDataBaseManager _dataBaseManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="DataBaseService"/>类型的新实例.
    /// </summary>
    public DataBaseService(
        ISqlSugarRepository<DbLinkEntity> repository,
        IDbLinkService dbLinkService,
        IFileManager fileManager,
        IDataBaseManager dataBaseManager,
        IUserManager userManager)
    {
        _repository = repository;
        _dbLinkService = dbLinkService;
        _fileManager = fileManager;
        _dataBaseManager = dataBaseManager;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 表名列表.
    /// </summary>
    /// <param name="id">连接Id.</param>
    /// <param name="input">过滤条件.</param>
    /// <returns></returns>
    [HttpGet("{id}/Tables")]
    public async Task<dynamic> GetList(string id, [FromQuery] PageInputBase input)
    {
        var link = await _dbLinkService.GetInfo(id);
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        if (!_dataBaseManager.IsConnection(tenantLink)) throw Oops.Oh(ErrorCode.D1507);
        var tables = _dataBaseManager.GetTableInfos(tenantLink);
        tables = tables.Where((x, i) => tables.FindIndex(z => z.Name == x.Name) == i).ToList();//去重
        var output = tables.Adapt<List<DatabaseTableListOutput>>();
        if (!string.IsNullOrEmpty(input.keyword))
            output = output.FindAll(d => d.table.ToLower().Contains(input.keyword.ToLower()) || (d.tableName.IsNotEmptyOrNull() && d.tableName.ToLower().Contains(input.keyword.ToLower())));
        return PageResult<DatabaseTableListOutput>.SqlSugarPageResult(new SqlSugarPagedList<DatabaseTableListOutput>()
        {
            list = output.OrderBy(x => x.table).Skip((input.currentPage - 1) * input.pageSize).Take(input.pageSize).OrderBy(x => x.table).ToList(),
            pagination = new Pagination()
            {
                CurrentPage = input.currentPage,
                PageSize = input.pageSize,
                Total = output.Count
            }
        });
    }

    /// <summary>
    /// 预览数据.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <param name="linkId">连接Id.</param>
    /// <param name="tableName">表名.</param>
    /// <returns></returns>
    [HttpGet("{linkId}/Table/{tableName}/Preview")]
    public async Task<dynamic> GetData([FromQuery] DatabaseTablePreviewQuery input, string linkId, string tableName)
    {
        var link = await _dbLinkService.GetInfo(linkId);
        if (string.IsNullOrEmpty(tableName)) return new PageResult();
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        StringBuilder dbSql = new StringBuilder();
        dbSql.AppendFormat("SELECT * FROM {0} WHERE 1=1", tableName);
        if (!string.IsNullOrEmpty(input.field) && !string.IsNullOrEmpty(input.keyword))
            dbSql.AppendFormat(" AND {0} like '%{1}%'", input.field, input.keyword);

        return await _dataBaseManager.GetDataTablePage(tenantLink, dbSql.ToString(), input.currentPage, input.pageSize);
    }

    /// <summary>
    /// 字段列表.
    /// </summary>
    /// <param name="linkId">连接Id.</param>
    /// <param name="tableName">表名.</param>
    /// <param name="type">字段类型.</param>
    /// <returns></returns>
    [HttpGet("{linkId}/Tables/{tableName}/Fields")]
    public async Task<dynamic> GetFieldList(string linkId, string tableName, [FromQuery] string type)
    {
        var link = await _dbLinkService.GetInfo(linkId);
        if (string.IsNullOrEmpty(tableName)) return new PageResult();
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        if (!_dataBaseManager.IsConnection(tenantLink)) throw Oops.Oh(ErrorCode.D1507);
        if (!_dataBaseManager.IsAnyTable(tenantLink, tableName)) throw Oops.Oh(ErrorCode.D1521);
        var data = _dataBaseManager.GetFieldList(tenantLink, tableName).Adapt<List<TableFieldOutput>>();
        if (type.Equals("1"))
        {
            data.ForEach(item =>
            {
                item.field = item.field.ReplaceRegex("^f_", string.Empty).ParseToPascalCase().ToLowerCase();
            });
        }

        return new { list = data };
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="linkId">连接Id.</param>
    /// <param name="tableName">主键值.</param>
    /// <returns></returns>
    [HttpGet("{linkId}/Table/{tableName}")]
    public async Task<dynamic> GetInfo(string linkId, string tableName)
    {
        var link = await _dbLinkService.GetInfo(linkId);
        if (string.IsNullOrEmpty(tableName)) return new PageResult();
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        var output = _dataBaseManager.GetDataBaseTableInfo(tenantLink, tableName);
        output.hasTableData = _dataBaseManager.IsAnyData(tenantLink, tableName);
        return output;
    }

    /// <summary>
    /// 获取数据库表字段下拉框列表(弃用).
    /// </summary>
    /// <param name="linkId">连接Id.</param>
    /// <param name="tableName">表名.</param>
    /// <returns></returns>
    [HttpGet("{linkId}/Tables/{tableName}/Fields/Selector")]
    public async Task<dynamic> SelectorData(string linkId, string tableName)
    {
        var link = await _dbLinkService.GetInfo(linkId);
        if (string.IsNullOrEmpty(tableName)) return new PageResult();
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        var data = _dataBaseManager.GetDBTableList(tenantLink).FindAll(m => m.table == tableName).Adapt<List<DatabaseTableFieldsSelectorOutput>>();
        return new { list = data };
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="linkId">连接ID.</param>
    /// <param name="tableName">表名称.</param>
    /// <returns></returns>
    [HttpGet("{linkId}/Table/{tableName}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string linkId, string tableName)
    {
        var link = await _dbLinkService.GetInfo(linkId);
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        var data = _dataBaseManager.GetDataBaseTableInfo(tenantLink, tableName);
        var jsonStr = data.ToJsonString();
        return await _fileManager.Export(jsonStr, data.tableInfo.table, ExportFileType.bdb);
    }

    /// <summary>
    /// 表名列表(包含视图).
    /// </summary>
    /// <param name="id">连接Id.</param>
    /// <param name="input">过滤条件.</param>
    /// <returns></returns>
    [HttpGet("{id}/TableAll")]
    public async Task<dynamic> GetListAll(string id, [FromQuery] KeywordInput input)
    {
        try
        {
            var link = await _dbLinkService.GetInfo(id);
            var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
            var tables = _dataBaseManager.GetTableInfos(tenantLink);
            tables = tables.Where((x, i) => tables.FindIndex(z => z.Name == x.Name) == i).ToList();//去重
            var list1 = tables.Adapt<List<DatabaseTableListOutput>>();
            if (!string.IsNullOrEmpty(input.keyword))
                list1 = list1.FindAll(d => d.table.ToLower().Contains(input.keyword.ToLower()) || (d.tableName.IsNotEmptyOrNull() && d.tableName.ToLower().Contains(input.keyword.ToLower())));
            var views = _dataBaseManager.GetTableInfos(tenantLink, true);
            views = views.Where((x, i) => views.FindIndex(z => z.Name == x.Name) == i).ToList();//去重
            var list2 = views.Adapt<List<DatabaseTableListOutput>>();
            foreach (var item in list2) item.type = 1;
            if (!string.IsNullOrEmpty(input.keyword))
                list2 = list2.FindAll(d => d.table.ToLower().Contains(input.keyword.ToLower()) || (d.tableName.IsNotEmptyOrNull() && d.tableName.ToLower().Contains(input.keyword.ToLower())));
            var output = list1.OrderBy(x => x.table).ToList().Union(list2.OrderBy(x => x.table).ToList()).ToList();
            return new { list = output };
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.D1507);
        }
    }
    #endregion

    #region POST

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="linkId">连接Id.</param>
    /// <param name="tableName">表名.</param>
    /// <returns></returns>
    [HttpDelete("{linkId}/Table/{tableName}")]
    [UnitOfWork]
    public async Task Delete(string linkId, string tableName)
    {
        if (IsSysTable(tableName))
            throw Oops.Oh(ErrorCode.D1504);
        var link = await _dbLinkService.GetInfo(linkId);
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        var data = _dataBaseManager.GetData(tenantLink, tableName);
        if (data.Rows.Count > 0)
            throw Oops.Oh(ErrorCode.D1508);
        if (!_dataBaseManager.Delete(tenantLink, tableName))
            throw Oops.Oh(ErrorCode.D1500);
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="linkId">连接Id.</param>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("{linkId}/Table")]
    [UnitOfWork]
    public async Task Create(string linkId, [FromBody] DatabaseTableInfoOutput input)
    {
        var link = await _dbLinkService.GetInfo(linkId);
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        if (_dataBaseManager.IsAnyTable(tenantLink, input.tableInfo.newTable))
            throw Oops.Oh(ErrorCode.D1503);
        if (input.tableInfo.newTable.Length >= 30) throw Oops.Oh(ErrorCode.D1514);
        if (input.tableFieldList.Any(x => x.field.Length >= 30)) throw Oops.Oh(ErrorCode.D1515);
        var tableInfo = input.tableInfo.Adapt<DbTableModel>();
        tableInfo.table = input.tableInfo.newTable;
        var tableFieldList = input.tableFieldList.Adapt<List<DbTableFieldModel>>();
        foreach (var tableField in tableFieldList)
        {
            if (tableField.dataType.Equals("decimal")) tableField.decimalDigits = 3;
        }

        try
        {
            if (!(await _dataBaseManager.Create(tenantLink, tableInfo, tableFieldList)))
                throw Oops.Oh(ErrorCode.D1501);
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="linkId">连接Id.</param>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPut("{linkId}/Table")]
    [UnitOfWork]
    public async Task Update(string linkId, [FromBody] DatabaseTableUpInput input)
    {
        var link = await _dbLinkService.GetInfo(linkId);
        if (input.tableInfo.newTable.Length >= 30) throw Oops.Oh(ErrorCode.D1514);
        if (input.tableFieldList.Any(x => x.field.Length >= 30)) throw Oops.Oh(ErrorCode.D1515);
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        var oldFieldList = _dataBaseManager.GetFieldList(tenantLink, input.tableInfo.table).Adapt<List<TableFieldOutput>>();
        oldFieldList = _dataBaseManager.ViewDataTypeConversion(oldFieldList, _dataBaseManager.ToDbType(tenantLink.DbType));
        var oldTableInfo = _dataBaseManager.GetTableInfos(tenantLink).Find(m => m.Name == input.tableInfo.table).Adapt<DbTableModel>();
        var data = _dataBaseManager.GetData(tenantLink, input.tableInfo.table);
        if (data.Rows.Count > 0)
            throw Oops.Oh(ErrorCode.D1508);
        var tableInfo = input.tableInfo.Adapt<DbTableModel>();
        tableInfo.table = input.tableInfo.newTable;
        if (IsSysTable(tableInfo.table))
            throw Oops.Oh(ErrorCode.D1517);
        var tableFieldList = input.tableFieldList.Adapt<List<DbTableFieldModel>>();
        foreach (var tableField in tableFieldList)
        {
            if (tableField.dataType.Equals("decimal")) tableField.decimalDigits = 3;
        }

        if (!input.tableInfo.table.ToLower().Equals(input.tableInfo.newTable.ToLower()) && _dataBaseManager.IsAnyTable(tenantLink, input.tableInfo.newTable))
            throw Oops.Oh(ErrorCode.D1503);
        try
        {
            if (!(await _dataBaseManager.Update(tenantLink, input.tableInfo.table, tableInfo, tableFieldList)))
            {
                await _dataBaseManager.Create(tenantLink, oldTableInfo, oldFieldList.Adapt<List<DbTableFieldModel>>());
                throw Oops.Oh(ErrorCode.D1502);
            }
        }
        catch (AppFriendlyException ex)
        {
            await _dataBaseManager.Create(tenantLink, oldTableInfo, oldFieldList.Adapt<List<DbTableFieldModel>>());
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="linkId">连接Id.</param>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPut("{linkId}/addFields")]
    [UnitOfWork]
    public async Task AddFields(string linkId, [FromBody] DatabaseTableUpInput input)
    {
        try
        {
            if (input.tableFieldList.Any(x => x.field.Length >= 30)) throw Oops.Oh(ErrorCode.D1515);
            var link = await _dbLinkService.GetInfo(linkId);
            var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
            await _dataBaseManager.AddTableColumn(tenantLink, input.tableInfo, input.tableFieldList.Adapt<List<DbTableFieldModel>>());
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 导入.
    /// </summary>
    /// <param name="linkid"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("{linkid}/Actions/Import")]
    public async Task ActionsImport(string linkid, IFormFile file)
    {
        var fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.bdb.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        var data = josn.ToObject<DatabaseTableInfoOutput>();
        if (data == null || data.tableFieldList == null || data.tableInfo == null)
            throw Oops.Oh(ErrorCode.D3006);
        data.tableInfo.newTable = data.tableInfo.table;
        await Create(linkid, data);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 是否系统表.
    /// </summary>
    /// <param name="table"></param>
    /// <returns></returns>
    private bool IsSysTable(string table)
    {
        string[] byoTable =
        {
            "base_advanced_query_scheme",
            "base_app_data",
            "base_authorize",
            "base_bill_num",
            "base_bill_rule",
            "base_columns_purview",
            "base_common_fields",
            "base_common_words",
            "base_data_interface",
            "base_data_interface_log",
            "base_data_interface_oauth",
            "base_data_interface_user",
            "base_data_interface_variate",
            "base_data_set",
            "base_db_link",
            "base_dictionary_data",
            "base_dictionary_type",
            "base_file",
            "base_group",
            "base_im_content",
            "base_im_reply",
            "base_integrate",
            "base_integrate_node",
            "base_integrate_queue",
            "base_integrate_task",
            "base_inventory_report",
            "base_language",
            "base_message",
            "base_module",
            "base_module_authorize",
            "base_module_button",
            "base_module_column",
            "base_module_data",
            "base_module_form",
            "base_module_link",
            "base_module_scheme",
            "base_msg_account",
            "base_msg_monitor",
            "base_msg_send",
            "base_msg_send_template",
            "base_msg_short_link",
            "base_msg_sms_field",
            "base_msg_template",
            "base_msg_template_param",
            "base_msg_wechat_user",
            "base_notice",
            "base_organize",
            "base_organize_administrator",
            "base_organize_relation",
            "base_permission_group",
            "base_portal",
            "base_portal_data",
            "base_portal_manage",
            "base_position",
            "base_print_log",
            "base_print_template",
            "base_print_version",
            "base_province",
            "base_province_atlas",
            "base_role",
            "base_schedule",
            "base_schedule_log",
            "base_schedule_user",
            "base_sign_img",
            "base_signature",
            "base_signature_user",
            "base_socials_users",
            "base_syn_third_info",
            "base_sys_config",
            "base_sys_log",
            "base_system",
            "base_time_task",
            "base_time_task_log",
            "base_user",
            "base_user_device",
            "base_user_old_password",
            "base_user_relation",
            "base_visual_alias",
            "base_visual_dev",
            "base_visual_filter",
            "base_visual_kit",
            "base_visual_link",
            "base_visual_log",
            "base_visual_release",
            "blade_visual",
            "blade_visual_assets",
            "blade_visual_category",
            "blade_visual_component",
            "blade_visual_config",
            "blade_visual_db",
            "blade_visual_glob",
            "blade_visual_map",
            "blade_visual_record",
            "ext_big_data",
            "ext_document",
            "ext_document_log",
            "ext_document_share",
            "ext_email_config",
            "ext_email_receive",
            "ext_email_send",
            "ext_employee",
            "ext_order",
            "ext_order_entry",
            "ext_order_receivable",
            "ext_product",
            "ext_product_classify",
            "ext_product_entry",
            "ext_product_goods",
            "ext_project_gantt",
            "ext_table_example",
            "ext_work_log",
            "ext_work_log_share",
            "flow_candidates",
            "flow_comment",
            "flow_delegate",
            "flow_event_log",
            "flow_form",
            "flow_form_authorize",
            "flow_form_relation",
            "flow_launch_user",
            "flow_reject_data",
            "flow_task",
            "flow_task_circulate",
            "flow_task_node",
            "flow_task_operator",
            "flow_task_operator_record",
            "flow_task_operator_user",
            "flow_template",
            "flow_template_json",
            "flow_visible",
            "wform_applybanquet",
            "wform_applydelivergoods",
            "wform_applydelivergoodsentry",
            "wform_applymeeting",
            "wform_archivalborrow",
            "wform_articleswarehous",
            "wform_batchpack",
            "wform_batchtable",
            "wform_conbilling",
            "wform_contractapproval",
            "wform_contractapprovalsheet",
            "wform_debitbill",
            "wform_documentapproval",
            "wform_documentsigning",
            "wform_expenseexpenditure",
            "wform_finishedproduct",
            "wform_finishedproductentry",
            "wform_incomerecognition",
            "wform_leaveapply",
            "wform_letterservice",
            "wform_materialrequisition",
            "wform_materialrequisitionentry",
            "wform_monthlyreport",
            "wform_officesupplies",
            "wform_outboundorder",
            "wform_outboundorderentry",
            "wform_outgoingapply",
            "wform_paydistribution",
            "wform_paymentapply",
            "wform_postbatchtab",
            "wform_procurementmaterial",
            "wform_procurementmaterialentry",
            "wform_purchaselist",
            "wform_purchaselistentry",
            "wform_quotationapproval",
            "wform_receiptprocessing",
            "wform_receiptsign",
            "wform_rewardpunishment",
            "wform_salesorder",
            "wform_salesorderentry",
            "wform_salessupport",
            "wform_staffovertime",
            "wform_supplementcard",
            "wform_travelapply",
            "wform_travelreimbursement",
            "wform_vehicleapply",
            "wform_violationhandling",
            "wform_warehousereceipt",
            "wform_warehousereceiptentry",
            "wform_workcontactsheet",
            "workflow_candidates",
            "workflow_circulate",
            "workflow_comment",
            "workflow_common",
            "workflow_delegate",
            "workflow_delegate_info",
            "workflow_form_relation",
            "workflow_launch_user",
            "workflow_node",
            "workflow_node_record",
            "workflow_operator",
            "workflow_record",
            "workflow_reject_data",
            "workflow_revoke",
            "workflow_task",
            "workflow_task_line",
            "workflow_template",
            "workflow_trigger_record",
            "workflow_trigger_task",
            "workflow_version"
        };
        return ((IList)byoTable).Contains(table.ToLower());
    }

    /// <summary>
    /// 获取表条数.
    /// </summary>
    /// <param name="tableList"></param>
    private void GetTableCount(List<DatabaseTableListOutput> tableList, DbLinkEntity link)
    {
        var _sqlSugarClient = _dataBaseManager.ChangeDataBase(link);
        foreach (var item in tableList)
        {
            try
            {
                item.sum = _sqlSugarClient.Queryable<dynamic>().AS(item.table).Count();
            }
            catch (Exception)
            {
                item.sum = 0;
            }
        }
    }

    #endregion
}