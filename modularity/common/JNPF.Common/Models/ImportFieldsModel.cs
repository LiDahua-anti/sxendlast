using JNPF.DependencyInjection;

namespace JNPF.Common.Models;

/// <summary>
/// 代码生成-导入 控件配置属性.
/// </summary>
public class CodeGenFieldsModel
{
    /// <summary>
    /// 设置默认值为空字符串.
    /// </summary>
    public string __vModel__ { get; set; } = string.Empty;

    /// <summary>
    /// 层级.
    /// </summary>
    public int level { get; set; }

    /// <summary>
    /// 最小值.
    /// </summary>
    public int? min { get; set; }

    /// <summary>
    /// 最大值.
    /// </summary>
    public int? max { get; set; }

    /// <summary>
    /// 评分控件最大值.
    /// </summary>
    public int? count { get; set; }

    /// <summary>
    /// 是否允许半选.
    /// </summary>
    // [JsonProperty(propertyName: "allow-half")]
    public bool allowHalf { get; set; }

    /// <summary>
    /// 单行输入、多行输入 最多字符.
    /// </summary>
    public string maxlength { get; set; }

    /// <summary>
    /// 开关控件 属性 - 开启展示值.
    /// </summary>
    public string activeTxt { get; set; }

    /// <summary>
    /// 开关控件 属性 - 关闭展示值.
    /// </summary>
    public string inactiveTxt { get; set; }

    /// <summary>
    /// 显示绑定值的格式.
    /// </summary>
    public string format { get; set; }

    /// <summary>
    /// 是否多选.
    /// </summary>
    public bool multiple { get; set; }

    /// <summary>
    /// 选项分隔符.
    /// </summary>
    public string separator { get; set; }

    /// <summary>
    /// 配置.
    /// </summary>
    public string __config__ { get; set; }

    /// <summary>
    /// 配置选项.
    /// </summary>
    public string props { get; set; }

    /// <summary>
    /// 配置项.
    /// </summary>
    public string options { get; set; }

    /// <summary>
    /// 弹窗选择主键.
    /// </summary>
    public string propsValue { get; set; }

    /// <summary>
    /// 关联表单字段.
    /// </summary>
    public string relationField { get; set; }

    /// <summary>
    /// 关联表单id.
    /// </summary>
    public string modelId { get; set; }

    /// <summary>
    /// 数据接口ID.
    /// </summary>
    public string interfaceId { get; set; }

    /// <summary>
    /// 可选范围.
    /// </summary>
    public string selectType { get; set; }

    /// <summary>
    /// 新用户选择控件.
    /// </summary>
    public string ableIds { get; set; }
}

public class CodeGenChildsModel
{
    /// <summary>
    /// 设置默认值为空字符串.
    /// </summary>
    public string __vModel__ { get; set; } = string.Empty;

    /// <summary>
    /// 层级.
    /// </summary>
    public int level { get; set; }

    /// <summary>
    /// 最小值.
    /// </summary>
    public int? min { get; set; }

    /// <summary>
    /// 最大值.
    /// </summary>
    public int? max { get; set; }

    /// <summary>
    /// 开关控件 属性 - 开启展示值.
    /// </summary>
    public string activeTxt { get; set; }

    /// <summary>
    /// 开关控件 属性 - 关闭展示值.
    /// </summary>
    public string inactiveTxt { get; set; }

    /// <summary>
    /// 显示绑定值的格式.
    /// </summary>
    public string format { get; set; }

    /// <summary>
    /// 是否多选.
    /// </summary>
    public bool multiple { get; set; }

    /// <summary>
    /// 选项分隔符.
    /// </summary>
    public string separator { get; set; }

    /// <summary>
    /// 插槽.
    /// </summary>
    public CodeGenSlotModel __slot__ { get; set; }

    /// <summary>
    /// 配置.
    /// </summary>
    public CodeGenConfigModel __config__ { get; set; }

    /// <summary>
    /// 配置选项.
    /// </summary>
    public CodeGenPropsBeanModel props { get; set; }

    /// <summary>
    /// 配置项.
    /// </summary>
    public List<object> options { get; set; }

    /// <summary>
    /// 弹窗选择主键.
    /// </summary>
    public string propsValue { get; set; }

    /// <summary>
    /// 关联表单字段.
    /// </summary>
    public string relationField { get; set; }

    /// <summary>
    /// 关联表单id.
    /// </summary>
    public string modelId { get; set; }

    /// <summary>
    /// 数据接口ID.
    /// </summary>
    public string interfaceId { get; set; }

    /// <summary>
    /// 可选范围.
    /// </summary>
    public string selectType { get; set; }

    /// <summary>
    /// 新用户选择控件.
    /// </summary>
    public string ableIds { get; set; }
}

/// <summary>
/// 代码生成-插槽模型.
/// </summary>
public class CodeGenSlotModel
{
    /// <summary>
    /// 配置项.
    /// </summary>
    public List<Dictionary<string, object>> options { get; set; }
}

/// <summary>
/// 代码生成-配置模型.
/// </summary>
public class CodeGenConfigModel
{
    /// <summary>
    /// 关联表名.
    /// </summary>
    public string tableName { get; set; }

    /// <summary>
    /// 验证规则.
    /// </summary>
    public List<CodeGenRegListModel> regList { get; set; }

    /// <summary>
    /// jnpf识别符.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 单据规则必须填.
    /// </summary>
    public string rule { get; set; }

    /// <summary>
    /// 单据规则类型.
    /// </summary>
    public int? ruleType { get; set; }

    /// <summary>
    /// 数据字典类型.
    /// </summary>
    public string dictionaryType { get; set; }

    /// <summary>
    /// 是否必填.
    /// </summary>
    public bool required { get; set; }

    /// <summary>
    /// 是否唯一.
    /// </summary>
    public bool unique { get; set; }

    /// <summary>
    /// 控件标题名.
    /// </summary>
    public string label { get; set; }

    /// <summary>
    /// object数据类型 (static、 dictionary).
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 远端数据接口.
    /// </summary>
    public string propsUrl { get; set; }

    /// <summary>
    /// 子集.
    /// </summary>
    public List<CodeGenChildsModel> children { get; set; }

    /// <summary>
    /// 选项配置.
    /// </summary>
    public CodeGenPropsBeanModel props { get; set; }

    /// <summary>
    /// 系统控件 - 所属组织 属性 - 显示内容
    /// all ：显示组织， last ： 显示部门.
    /// </summary>
    public string showLevel { get; set; }

    /// <summary>
    /// 数据接口配置.
    /// </summary>
    public object templateJson { get; set; }

    /// <summary>
    /// 开始时间规则开关.
    /// </summary>
    public bool startTimeRule { get; set; } = false;

    /// <summary>
    /// 开始时间单位：1-年,2-月,3-日/1-时,2-分,3-秒.
    /// </summary>
    public int startTimeTarget { get; set; }

    /// <summary>
    /// 开始时间类型：1-特定时间,2-表单字段,3-填写当前时间,4-当前时间前,5-当前时间后.
    /// </summary>
    public int startTimeType { get; set; }

    /// <summary>
    /// 开始时间值.
    /// </summary>
    public string startTimeValue { get; set; }

    /// <summary>
    /// 开始时间关联字段.
    /// </summary>
    public string startRelationField { get; set; }

    /// <summary>
    /// 结束时间规则开关.
    /// </summary>
    public bool endTimeRule { get; set; } = false;

    /// <summary>
    /// 结束时间单位：1-年,2-月,3-日/1-时,2-分,3-秒.
    /// </summary>
    public int endTimeTarget { get; set; }

    /// <summary>
    /// 结束时间类型：1-特定时间,2-表单字段,3-填写当前时间,4-当前时间前,5-当前时间后.
    /// </summary>
    public int endTimeType { get; set; }

    /// <summary>
    /// 结束时间值.
    /// </summary>
    public string endTimeValue { get; set; }

    /// <summary>
    /// 结束时间关联字段.
    /// </summary>
    public string endRelationField { get; set; }

    /// <summary>
    /// 精度.
    /// </summary>
    public int? precision { get; set; }

    /// <summary>
    /// 控件ID.
    /// </summary>
    public string formId { get; set; }

    /// <summary>
    /// 单据规则配置.
    /// </summary>
    public RuleConfigModel ruleConfig { get; set; }

    /// <summary>
    /// 后端用 业务主键标识.
    /// </summary>
    public bool IsBusinessKey { get; set; }

    /// <summary>
    /// 后端用 业务主键 逻辑提示语.
    /// </summary>
    public string tag { get; set; }
}

/// <summary>
/// 代码生成-配置属性模型.
/// </summary>
public class CodeGenPropsBeanModel
{
    /// <summary>
    /// 指定选项标签为选项对象的某个属性值.
    /// </summary>
    public string label { get; set; }

    /// <summary>
    /// 指定选项的值为选项对象的某个属性值.
    /// </summary>
    public string value { get; set; }

    /// <summary>
    /// 指定选项的子选项为选项对象的某个属性值.
    /// </summary>
    public string children { get; set; }
}

/// <summary>
/// 验证规则模型.
/// </summary>
[SuppressSniffer]
public class CodeGenRegListModel
{
    /// <summary>
    /// 正则表达式.
    /// </summary>
    public string pattern { get; set; }

    /// <summary>
    /// 错误提示.
    /// </summary>
    public string message { get; set; }
}

/// <summary>
/// 配置选项模型.
/// </summary>
public class CodeGenPropsModel
{
    /// <summary>
    /// 指定选项标签为选项对象的某个属性值.
    /// </summary>
    public string label { get; set; }

    /// <summary>
    /// 指定选项的值为选项对象的某个属性值.
    /// </summary>
    public string value { get; set; }

    /// <summary>
    /// 指定选项的子选项为选项对象的某个属性值.
    /// </summary>
    public string children { get; set; }

}

public class RuleConfigModel
{
    /// <summary>
    /// 前缀.
    /// </summary>
    public List<FieldConfig> prefixList { get; set; }

    /// <summary>
    /// 方式设置.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 时间格式.
    /// </summary>
    public string dateFormat { get; set; }

    /// <summary>
    /// 起始值位数.
    /// </summary>
    public int? digit { get; set; }

    /// <summary>
    /// 起始值.
    /// </summary>
    public string startNumber { get; set; }

    /// <summary>
    /// 随机数位数.
    /// </summary>
    public int? randomDigit { get; set; }

    /// <summary>
    /// 随机数类型（1：数字，2：字母+数字）.
    /// </summary>
    public int? randomType { get; set; }

    /// <summary>
    /// 后缀.
    /// </summary>
    public List<FieldConfig> suffixList { get; set; }

}

public class FieldConfig
{
    /// <summary>
    /// 来源（1：字段，2：自定义）.
    /// </summary>
    public int? sourceType { get; set; }

    /// <summary>
    /// 值.
    /// </summary>
    public string relationField { get; set; }
}