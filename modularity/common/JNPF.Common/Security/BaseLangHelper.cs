using JNPF.Common.Configuration;
using JNPF.Common.Extension;

namespace JNPF.Common.Security;

/// <summary>
/// LangBaseHelper.
/// </summary>
public class BaseLangHelper
{
    /// <summary>
    /// 获取语言数据名称.
    /// </summary>
    /// <param name="language">语种.</param>
    /// <param name="code">编码.</param>
    /// <returns></returns>
    public static string GetDataName(string language, string code)
    {
        var result = string.Empty;
        var list = new List<Dictionary<string, object>>();
        var langFileName = string.Format("{0}.json", language);
        var path = Path.Combine(FileVariable.LangBasePath, langFileName);

        if (FileHelper.IsExistFile(path))
        {
            StreamReader sr = new StreamReader(path);
            var itemValue = sr.ReadToEnd().ToObject<Dictionary<string, object>>();
            sr.Close();

            if (itemValue.IsNotEmptyOrNull()) GetMultilingualValue(list, itemValue, language);

            if (list.Any(it => it["code"].Equals(code)))
                result = list.Find(it => it["code"].Equals(code))[language].ToString();
        }

        return result;
    }

    /// <summary>
    /// 递归获取多语言值.
    /// </summary>
    /// <param name="list">处理后的值.</param>
    /// <param name="value">文件的值.</param>
    /// <param name="lang">语言类型.</param>
    /// <param name="key">多语言的key值.</param>
    public static void GetMultilingualValue(List<Dictionary<string, object>> list, Dictionary<string, object> value, string lang, string? key = null)
    {
        foreach (var item in value)
        {
            var dic = new Dictionary<string, object>();
            var itemKey = item.Key;
            if (key.IsNotEmptyOrNull())
                itemKey = string.Format("{0}.{1}", key, item.Key);

            if (item.Value.ToString().StartsWith("{") && item.Value.ToString().EndsWith("}"))
            {
                GetMultilingualValue(list, item.Value.ToObject<Dictionary<string, object>>(), lang, itemKey);
            }
            else
            {
                dic.Add("code", itemKey);
                dic.Add(lang, item.Value);
                list.Add(dic);
            }
        }
    }
}
