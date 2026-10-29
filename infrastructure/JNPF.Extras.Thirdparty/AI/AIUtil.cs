using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Extras.Thirdparty.AI.Internal;
using JNPF.FriendlyException;
using JNPF.JsonSerialization;
using JNPF.RemoteRequest.Extensions;

namespace JNPF.Extras.Thirdparty.AI;

/// <summary>
/// 阿里云 AI.
/// </summary>
public class AIUtil
{
    private static readonly string? _host = App.Configuration["AI:ApiHost"];
    private static readonly string? _apiKey = App.Configuration["AI:ApiKey"];
    private static readonly string? _model = App.Configuration["AI:Model"];

    /// <summary>
    /// 发起 AI 请求.
    /// </summary>
    public static async Task<string> SendAIRequestAsync(List<string> userQuestionList, string? systemQuestion = null)
    {
        if (!App.Configuration["AI:Enabled"].ParseToBool()) throw Oops.Oh(ErrorCode.D3301);

        var output = string.Empty;
        try
        {
            var messages = new List<MessageModel>();
            if (systemQuestion.IsNotEmptyOrNull()) messages.Add(new MessageModel() { role = "system", content = systemQuestion });
            foreach (var userQuestion in userQuestionList)
            {
                if (userQuestion.Equals("JNPF是什么"))
                {
                    output = AIAnswerConst.WHATISIT;
                    return output;
                }
                else if (userQuestion.Equals("JNPF的性能特点"))
                {
                    output = AIAnswerConst.PERFORMANCECHARACTERISTICS;
                    return output;
                }
                else if (userQuestion.Equals("JNPF支持哪些数据库"))
                {
                    output = AIAnswerConst.SUPPORTEDDATABASES;
                    return output;
                }
                else
                {
                    messages.Add(new MessageModel() { role = "user", content = userQuestion });
                }
            }

            var parameter = new AIParameter() { model = _model, messages = messages };

            var path = string.Format("{0}v1/chat/completions", _host);
            var headers = new Dictionary<string, object>();
            headers.Add("Authorization", "Bearer " + _apiKey);

            var res = await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(headers).SetBody(parameter).PostAsStringAsync();
            output = res.ToObject<AIOutputModel>().choices.FirstOrDefault().message.content;
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.D3302);
        }

        return output;
    }
}