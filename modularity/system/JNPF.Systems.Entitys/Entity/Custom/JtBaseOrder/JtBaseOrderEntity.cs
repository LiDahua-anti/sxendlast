using JNPF.Common.CodeGenUpload;
using SqlSugar;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;

namespace JNPF.example.Entitys;

/// <summary>
/// 排产计划（二开）实体.
/// </summary>
[SugarTable("sx_base_order")]
public class JtBaseOrderEntity : ITenantFilter
{
    /// <summary>
    /// 主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_ID", IsPrimaryKey = true, ColumnDescription = "/// 主键. ///")]
    public string? id { get; set; }

    /// <summary>
    /// PPH（小时产能）V.
    /// </summary>
    [SugarColumn(ColumnName = "pph_value", ColumnDescription = "/// PPH（小时产能）V. ///")]
    [CodeGenUpload("pph_value", "pph_value", "{\r\n  \"tableName\": \"sx_base_order\",\r\n  \"regList\": [],\r\n  \"jnpfKey\": \"input\",\r\n  \"rule\": null,\r\n  \"ruleType\": null,\r\n  \"dictionaryType\": null,\r\n  \"required\": false,\r\n  \"unique\": false,\r\n  \"label\": \"PPH\",\r\n  \"dataType\": null,\r\n  \"propsUrl\": null,\r\n  \"children\": null,\r\n  \"props\": null,\r\n  \"showLevel\": null,\r\n  \"templateJson\": null,\r\n  \"startTimeRule\": false,\r\n  \"startTimeTarget\": 0,\r\n  \"startTimeType\": 0,\r\n  \"startTimeValue\": null,\r\n  \"startRelationField\": null,\r\n  \"endTimeRule\": false,\r\n  \"endTimeTarget\": 0,\r\n  \"endTimeType\": 0,\r\n  \"endTimeValue\": null,\r\n  \"endRelationField\": null,\r\n  \"precision\": null,\r\n  \"formId\": \"formItem8df2a8\",\r\n  \"ruleConfig\": null,\r\n  \"IsBusinessKey\": false,\r\n  \"tag\": \"JnpfInput\"\r\n}",9999999L)]
    public string? Pph_value { get; set; }

    /// <summary>
    /// CT  V.
    /// </summary>
    [SugarColumn(ColumnName = "ct_value", ColumnDescription = "/// CT V. ///")]
    [CodeGenUpload("ct_value", "ct_value", "{\r\n  \"tableName\": \"sx_base_order\",\r\n  \"regList\": [],\r\n  \"jnpfKey\": \"input\",\r\n  \"rule\": null,\r\n  \"ruleType\": null,\r\n  \"dictionaryType\": null,\r\n  \"required\": false,\r\n  \"unique\": false,\r\n  \"label\": \"CT\",\r\n  \"dataType\": null,\r\n  \"propsUrl\": null,\r\n  \"children\": null,\r\n  \"props\": null,\r\n  \"showLevel\": null,\r\n  \"templateJson\": null,\r\n  \"startTimeRule\": false,\r\n  \"startTimeTarget\": 0,\r\n  \"startTimeType\": 0,\r\n  \"startTimeValue\": null,\r\n  \"startRelationField\": null,\r\n  \"endTimeRule\": false,\r\n  \"endTimeTarget\": 0,\r\n  \"endTimeType\": 0,\r\n  \"endTimeValue\": null,\r\n  \"endRelationField\": null,\r\n  \"precision\": null,\r\n  \"formId\": \"formItemc8d823\",\r\n  \"ruleConfig\": null,\r\n  \"IsBusinessKey\": false,\r\n  \"tag\": \"JnpfInput\"\r\n}",9999999L)]
    public string? Ct_value { get; set; }

    /// <summary>
    /// MPPH  V.
    /// </summary>
    [SugarColumn(ColumnName = "mpph_value", ColumnDescription = "/// MPPH V. ///")]
    [CodeGenUpload("mpph_value", "mpph_value", "{\r\n  \"tableName\": \"sx_base_order\",\r\n  \"regList\": [],\r\n  \"jnpfKey\": \"input\",\r\n  \"rule\": null,\r\n  \"ruleType\": null,\r\n  \"dictionaryType\": null,\r\n  \"required\": false,\r\n  \"unique\": false,\r\n  \"label\": \"MPPH\",\r\n  \"dataType\": null,\r\n  \"propsUrl\": null,\r\n  \"children\": null,\r\n  \"props\": null,\r\n  \"showLevel\": null,\r\n  \"templateJson\": null,\r\n  \"startTimeRule\": false,\r\n  \"startTimeTarget\": 0,\r\n  \"startTimeType\": 0,\r\n  \"startTimeValue\": null,\r\n  \"startRelationField\": null,\r\n  \"endTimeRule\": false,\r\n  \"endTimeTarget\": 0,\r\n  \"endTimeType\": 0,\r\n  \"endTimeValue\": null,\r\n  \"endRelationField\": null,\r\n  \"precision\": null,\r\n  \"formId\": \"formItem71b2ca\",\r\n  \"ruleConfig\": null,\r\n  \"IsBusinessKey\": false,\r\n  \"tag\": \"JnpfInput\"\r\n}",9999999L)]
    public string? Mpph_value { get; set; }

    /// <summary>
    /// 产线V.
    /// </summary>
    [SugarColumn(ColumnName = "production_type", ColumnDescription = "/// 产线V. ///")]
    [CodeGenUpload("production_type", false, "{\r\n  \"label\": \"devicetype_name\",\r\n  \"value\": \"F_ID\",\r\n  \"children\": null\r\n}", "[\r\n  {\r\n    \"devicetype_code\": \"M_TYPE_1\",\r\n    \"F_ID\": \"2002986306514325504\"\r\n  },\r\n  {\r\n    \"devicetype_code\": \"M_TYPE_2\",\r\n    \"F_ID\": \"2002986326756036608\"\r\n  },\r\n  {\r\n    \"devicetype_code\": \"M_TYPE_3\",\r\n    \"F_ID\": \"2002986347241017344\"\r\n  },\r\n  {\r\n    \"devicetype_code\": \"M_TYPE_1_1\",\r\n    \"F_ID\": \"2002986398075981824\"\r\n  },\r\n  {\r\n    \"devicetype_code\": \"M_TYPE_1_2\",\r\n    \"F_ID\": \"2002986920698843136\"\r\n  },\r\n  {\r\n    \"devicetype_code\": \"M_TYPE_1_3\",\r\n    \"F_ID\": \"2002986989669978112\"\r\n  },\r\n  {\r\n    \"devicetype_code\": \"M_TYPE_1_4\",\r\n    \"F_ID\": \"2002987016945537024\"\r\n  },\r\n  {\r\n    \"devicetype_code\": \"M_TYPE_2_1\",\r\n    \"F_ID\": \"2002987096884776960\"\r\n  },\r\n  {\r\n    \"devicetype_code\": \"M_TYPE_3_1\",\r\n    \"F_ID\": \"2002987117734662144\"\r\n  },\r\n  {\r\n    \"devicetype_code\": \"M_TYPE_4\",\r\n    \"F_ID\": \"2009161406301212672\"\r\n  }\r\n]", "{\r\n  \"tableName\": \"sx_base_order\",\r\n  \"regList\": [],\r\n  \"jnpfKey\": \"select\",\r\n  \"rule\": null,\r\n  \"ruleType\": null,\r\n  \"dictionaryType\": \"\",\r\n  \"required\": false,\r\n  \"unique\": false,\r\n  \"label\": \"产线\",\r\n  \"dataType\": \"dynamic\",\r\n  \"propsUrl\": \"2013065159685312512\",\r\n  \"children\": null,\r\n  \"props\": null,\r\n  \"showLevel\": null,\r\n  \"templateJson\": [],\r\n  \"startTimeRule\": false,\r\n  \"startTimeTarget\": 0,\r\n  \"startTimeType\": 0,\r\n  \"startTimeValue\": null,\r\n  \"startRelationField\": null,\r\n  \"endTimeRule\": false,\r\n  \"endTimeTarget\": 0,\r\n  \"endTimeType\": 0,\r\n  \"endTimeValue\": null,\r\n  \"endRelationField\": null,\r\n  \"precision\": null,\r\n  \"formId\": \"formItemcb330a\",\r\n  \"ruleConfig\": null,\r\n  \"IsBusinessKey\": false,\r\n  \"tag\": \"JnpfSelect\"\r\n}")]
    public string? Production_type { get; set; }

    /// <summary>
    /// 班次V.
    /// </summary>
    [SugarColumn(ColumnName = "work_shift", ColumnDescription = "/// 班次V. ///")]
    [CodeGenUpload("work_shift", "work_shift", "2019223010992590848", "F_ID", "class_name", "", "{\r\n  \"tableName\": \"sx_base_order\",\r\n  \"regList\": [],\r\n  \"jnpfKey\": \"popupSelect\",\r\n  \"rule\": null,\r\n  \"ruleType\": null,\r\n  \"dictionaryType\": null,\r\n  \"required\": false,\r\n  \"unique\": false,\r\n  \"label\": \"班次\",\r\n  \"dataType\": null,\r\n  \"propsUrl\": null,\r\n  \"children\": null,\r\n  \"props\": null,\r\n  \"showLevel\": null,\r\n  \"templateJson\": null,\r\n  \"startTimeRule\": false,\r\n  \"startTimeTarget\": 0,\r\n  \"startTimeType\": 0,\r\n  \"startTimeValue\": null,\r\n  \"startRelationField\": null,\r\n  \"endTimeRule\": false,\r\n  \"endTimeTarget\": 0,\r\n  \"endTimeType\": 0,\r\n  \"endTimeValue\": null,\r\n  \"endRelationField\": null,\r\n  \"precision\": null,\r\n  \"formId\": \"formItem414225\",\r\n  \"ruleConfig\": null,\r\n  \"IsBusinessKey\": false,\r\n  \"tag\": \"JnpfPopupSelect\"\r\n}")]
    public string? Work_shift { get; set; }

    /// <summary>
    /// 开工日期V.
    /// </summary>
    [SugarColumn(ColumnName = "start_date", ColumnDescription = "/// 开工日期V. ///")]
    [CodeGenUpload("start_date", "yyyy-MM-dd", "{\r\n  \"tableName\": \"sx_base_order\",\r\n  \"regList\": [],\r\n  \"jnpfKey\": \"datePicker\",\r\n  \"rule\": null,\r\n  \"ruleType\": null,\r\n  \"dictionaryType\": null,\r\n  \"required\": false,\r\n  \"unique\": false,\r\n  \"label\": \"计划时间\",\r\n  \"dataType\": null,\r\n  \"propsUrl\": null,\r\n  \"children\": null,\r\n  \"props\": null,\r\n  \"showLevel\": null,\r\n  \"templateJson\": null,\r\n  \"startTimeRule\": false,\r\n  \"startTimeTarget\": 1,\r\n  \"startTimeType\": 1,\r\n  \"startTimeValue\": null,\r\n  \"startRelationField\": \"\",\r\n  \"endTimeRule\": false,\r\n  \"endTimeTarget\": 1,\r\n  \"endTimeType\": 1,\r\n  \"endTimeValue\": null,\r\n  \"endRelationField\": \"\",\r\n  \"precision\": null,\r\n  \"formId\": \"formItem133770\",\r\n  \"ruleConfig\": null,\r\n  \"IsBusinessKey\": false,\r\n  \"tag\": \"JnpfDatePicker\"\r\n}")]
    public DateTime? Start_date { get; set; }

    /// <summary>
    /// 完成品编码/型番V.
    /// </summary>
    [SugarColumn(ColumnName = "product_code", ColumnDescription = "/// 完成品编码/型番V. ///")]
    [CodeGenUpload("product_code", "product_code", "{\r\n  \"tableName\": \"sx_base_order\",\r\n  \"regList\": [],\r\n  \"jnpfKey\": \"input\",\r\n  \"rule\": null,\r\n  \"ruleType\": null,\r\n  \"dictionaryType\": null,\r\n  \"required\": false,\r\n  \"unique\": false,\r\n  \"label\": \"完成品编码\",\r\n  \"dataType\": null,\r\n  \"propsUrl\": null,\r\n  \"children\": null,\r\n  \"props\": null,\r\n  \"showLevel\": null,\r\n  \"templateJson\": null,\r\n  \"startTimeRule\": false,\r\n  \"startTimeTarget\": 0,\r\n  \"startTimeType\": 0,\r\n  \"startTimeValue\": null,\r\n  \"startRelationField\": null,\r\n  \"endTimeRule\": false,\r\n  \"endTimeTarget\": 0,\r\n  \"endTimeType\": 0,\r\n  \"endTimeValue\": null,\r\n  \"endRelationField\": null,\r\n  \"precision\": null,\r\n  \"formId\": \"formItem9b6a6f\",\r\n  \"ruleConfig\": null,\r\n  \"IsBusinessKey\": false,\r\n  \"tag\": \"JnpfInput\"\r\n}",9999999L)]
    public string Product_code { get; set; }

    /// <summary>
    /// 完成品型号V.
    /// </summary>
    [SugarColumn(ColumnName = "product_name", ColumnDescription = "/// 完成品型号V. ///")]
    [CodeGenUpload("product_name", "product_name", "{\r\n  \"tableName\": \"sx_base_order\",\r\n  \"regList\": [],\r\n  \"jnpfKey\": \"input\",\r\n  \"rule\": null,\r\n  \"ruleType\": null,\r\n  \"dictionaryType\": null,\r\n  \"required\": false,\r\n  \"unique\": false,\r\n  \"label\": \"完成品型号\",\r\n  \"dataType\": null,\r\n  \"propsUrl\": null,\r\n  \"children\": null,\r\n  \"props\": null,\r\n  \"showLevel\": null,\r\n  \"templateJson\": null,\r\n  \"startTimeRule\": false,\r\n  \"startTimeTarget\": 0,\r\n  \"startTimeType\": 0,\r\n  \"startTimeValue\": null,\r\n  \"startRelationField\": null,\r\n  \"endTimeRule\": false,\r\n  \"endTimeTarget\": 0,\r\n  \"endTimeType\": 0,\r\n  \"endTimeValue\": null,\r\n  \"endRelationField\": null,\r\n  \"precision\": null,\r\n  \"formId\": \"formItem0335e3\",\r\n  \"ruleConfig\": null,\r\n  \"IsBusinessKey\": false,\r\n  \"tag\": \"JnpfInput\"\r\n}",9999999L)]
    public string? Product_name { get; set; }

    /// <summary>
    /// 营业口座 V.
    /// </summary>
    [SugarColumn(ColumnName = "open_no", ColumnDescription = "/// 营业口座 V. ///")]
    [CodeGenUpload("open_no", "open_no", "{\r\n  \"tableName\": \"sx_base_order\",\r\n  \"regList\": [],\r\n  \"jnpfKey\": \"input\",\r\n  \"rule\": null,\r\n  \"ruleType\": null,\r\n  \"dictionaryType\": null,\r\n  \"required\": false,\r\n  \"unique\": false,\r\n  \"label\": \"营业口座\",\r\n  \"dataType\": null,\r\n  \"propsUrl\": null,\r\n  \"children\": null,\r\n  \"props\": null,\r\n  \"showLevel\": null,\r\n  \"templateJson\": null,\r\n  \"startTimeRule\": false,\r\n  \"startTimeTarget\": 0,\r\n  \"startTimeType\": 0,\r\n  \"startTimeValue\": null,\r\n  \"startRelationField\": null,\r\n  \"endTimeRule\": false,\r\n  \"endTimeTarget\": 0,\r\n  \"endTimeType\": 0,\r\n  \"endTimeValue\": null,\r\n  \"endRelationField\": null,\r\n  \"precision\": null,\r\n  \"formId\": \"formItem20a711\",\r\n  \"ruleConfig\": null,\r\n  \"IsBusinessKey\": false,\r\n  \"tag\": \"JnpfInput\"\r\n}",9999999L)]
    public string? Open_no { get; set; }

    /// <summary>
    /// NC物料编码 V.
    /// </summary>
    [SugarColumn(ColumnName = "nc_bomNo", ColumnDescription = "/// NC物料编码 V. ///")]
    [CodeGenUpload("nc_bomNo", "nc_bomNo", "{\r\n  \"tableName\": \"sx_base_order\",\r\n  \"regList\": [],\r\n  \"jnpfKey\": \"input\",\r\n  \"rule\": null,\r\n  \"ruleType\": null,\r\n  \"dictionaryType\": null,\r\n  \"required\": false,\r\n  \"unique\": false,\r\n  \"label\": \"NC物料编码\",\r\n  \"dataType\": null,\r\n  \"propsUrl\": null,\r\n  \"children\": null,\r\n  \"props\": null,\r\n  \"showLevel\": null,\r\n  \"templateJson\": null,\r\n  \"startTimeRule\": false,\r\n  \"startTimeTarget\": 0,\r\n  \"startTimeType\": 0,\r\n  \"startTimeValue\": null,\r\n  \"startRelationField\": null,\r\n  \"endTimeRule\": false,\r\n  \"endTimeTarget\": 0,\r\n  \"endTimeType\": 0,\r\n  \"endTimeValue\": null,\r\n  \"endRelationField\": null,\r\n  \"precision\": null,\r\n  \"formId\": \"formItemebd183\",\r\n  \"ruleConfig\": null,\r\n  \"IsBusinessKey\": false,\r\n  \"tag\": \"JnpfInput\"\r\n}",9999999L)]
    public string? Nc_bomNo { get; set; }

    /// <summary>
    /// 计划数量V.
    /// </summary>
    [SugarColumn(ColumnName = "quantity", ColumnDescription = "/// 计划数量V. ///")]
    [CodeGenUpload("quantity", "quantity", "{\r\n  \"tableName\": \"sx_base_order\",\r\n  \"regList\": [],\r\n  \"jnpfKey\": \"input\",\r\n  \"rule\": null,\r\n  \"ruleType\": null,\r\n  \"dictionaryType\": null,\r\n  \"required\": false,\r\n  \"unique\": false,\r\n  \"label\": \"计划数量\",\r\n  \"dataType\": null,\r\n  \"propsUrl\": null,\r\n  \"children\": null,\r\n  \"props\": null,\r\n  \"showLevel\": null,\r\n  \"templateJson\": null,\r\n  \"startTimeRule\": false,\r\n  \"startTimeTarget\": 0,\r\n  \"startTimeType\": 0,\r\n  \"startTimeValue\": null,\r\n  \"startRelationField\": null,\r\n  \"endTimeRule\": false,\r\n  \"endTimeTarget\": 0,\r\n  \"endTimeType\": 0,\r\n  \"endTimeValue\": null,\r\n  \"endRelationField\": null,\r\n  \"precision\": null,\r\n  \"formId\": \"formItemabe189\",\r\n  \"ruleConfig\": null,\r\n  \"IsBusinessKey\": false,\r\n  \"tag\": \"JnpfInput\"\r\n}",9999999L)]
    public string? Quantity { get; set; }

    /// <summary>
    /// 生产状态(0未生产 1生产中 2已完成).
    /// </summary>
    [SugarColumn(ColumnName = "production_status", ColumnDescription = "/// 生产状态(0未生产 1生产中 2已完成). ///")]
    public int? Production_status { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_CREATOR_TIME", ColumnDescription = "/// 创建时间. ///")]
    [CodeGenUpload("F_CREATOR_TIME", "{\r\n  \"tableName\": \"sx_base_order\",\r\n  \"regList\": null,\r\n  \"jnpfKey\": \"createTime\",\r\n  \"rule\": null,\r\n  \"ruleType\": null,\r\n  \"dictionaryType\": null,\r\n  \"required\": false,\r\n  \"unique\": false,\r\n  \"label\": \"创建时间\",\r\n  \"dataType\": null,\r\n  \"propsUrl\": null,\r\n  \"children\": null,\r\n  \"props\": null,\r\n  \"showLevel\": null,\r\n  \"templateJson\": null,\r\n  \"startTimeRule\": false,\r\n  \"startTimeTarget\": 0,\r\n  \"startTimeType\": 0,\r\n  \"startTimeValue\": null,\r\n  \"startRelationField\": null,\r\n  \"endTimeRule\": false,\r\n  \"endTimeTarget\": 0,\r\n  \"endTimeType\": 0,\r\n  \"endTimeValue\": null,\r\n  \"endRelationField\": null,\r\n  \"precision\": null,\r\n  \"formId\": \"formItem732ab3\",\r\n  \"ruleConfig\": null,\r\n  \"IsBusinessKey\": false,\r\n  \"tag\": \"JnpfOpenData\"\r\n}")]
    public DateTime? F_CREATOR_TIME { get; set; }

    /// <summary>
    /// 创建用户.
    /// </summary>
    [SugarColumn(ColumnName = "F_CREATOR_USER_ID", ColumnDescription = "/// 创建用户. ///")]
    [CodeGenUpload("F_CREATOR_USER_ID", "{\r\n  \"tableName\": \"sx_base_order\",\r\n  \"regList\": null,\r\n  \"jnpfKey\": \"createUser\",\r\n  \"rule\": null,\r\n  \"ruleType\": null,\r\n  \"dictionaryType\": null,\r\n  \"required\": false,\r\n  \"unique\": false,\r\n  \"label\": \"创建用户\",\r\n  \"dataType\": null,\r\n  \"propsUrl\": null,\r\n  \"children\": null,\r\n  \"props\": null,\r\n  \"showLevel\": null,\r\n  \"templateJson\": null,\r\n  \"startTimeRule\": false,\r\n  \"startTimeTarget\": 0,\r\n  \"startTimeType\": 0,\r\n  \"startTimeValue\": null,\r\n  \"startRelationField\": null,\r\n  \"endTimeRule\": false,\r\n  \"endTimeTarget\": 0,\r\n  \"endTimeType\": 0,\r\n  \"endTimeValue\": null,\r\n  \"endRelationField\": null,\r\n  \"precision\": null,\r\n  \"formId\": \"formItemef52de\",\r\n  \"ruleConfig\": null,\r\n  \"IsBusinessKey\": false,\r\n  \"tag\": \"JnpfOpenData\"\r\n}")]
    public string? F_CREATOR_USER_ID { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    [SugarColumn(ColumnName = "F_SORT_CODE", ColumnDescription = "/// 排序码. ///")]
    public long? F_SORT_CODE { get; set; }

    /// <summary>
    /// 班次类型.
    /// </summary>
    [SugarColumn(ColumnName = "class_type", ColumnDescription = "/// 班次类型. ///")]
    public string? Class_type { get; set; }

    /// <summary>
    /// 时间.
    /// </summary>
    [SugarColumn(ColumnName = "class_time", ColumnDescription = "/// 时间. ///")]
    public string? Class_time { get; set; }

    /// <summary>
    /// 计划开始时间.
    /// </summary>
    [SugarColumn(ColumnName = "start_time", ColumnDescription = "/// 计划开始时间. ///")]
    public string? Start_time { get; set; }

    /// <summary>
    /// 计划结束时间.
    /// </summary>
    [SugarColumn(ColumnName = "end_time", ColumnDescription = "/// 计划结束时间. ///")]
    public string? End_time { get; set; }

    /// <summary>
    /// 实际开始时间.
    /// </summary>
    [SugarColumn(ColumnName = "actural_start_time", ColumnDescription = "/// 实际开始时间. ///")]
    public string? Actural_start_time { get; set; }

    /// <summary>
    /// 实际结束时间.
    /// </summary>
    [SugarColumn(ColumnName = "actural_end_time", ColumnDescription = "/// 实际结束时间. ///")]
    public string? Actural_end_time { get; set; }

    /// <summary>
    /// 流程引擎ID.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID", ColumnDescription = "/// 流程引擎ID. ///")]
    public string FlowId { get; set; }
    
    /// <summary>
    /// 获取或设置 租户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id", IsPrimaryKey = true)]
    public string TenantId { get; set; }
}