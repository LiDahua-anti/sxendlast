using JNPF.Common.Configuration;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DataEncryption;
using System.Text;

namespace JNPF.API.Entry.Handlers;

public class EncryptionHandler
{
    private readonly RequestDelegate _next;

    private readonly AppOptions _appOptions = App.GetConfig<AppOptions>("JNPF_App", true); // aes密钥

    public EncryptionHandler(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!(_appOptions.EncryptionPath.Contains(context.Request.Path.Value) && context.Request.Method == "POST"))
        {
            await _next(context);
            return;
        }

        // 拦截请求，保存原始的请求体
        var originalRequestBody = context.Request.Body;
        try
        {
            // 使请求体流支持查找
            context.Request.EnableBuffering();

            // 读取并解密请求体
            string bodyAsString;
            using (var streamReader = new StreamReader(context.Request.Body, leaveOpen: true))
            {
                bodyAsString = (await streamReader.ReadToEndAsync()).ToObject<encryptModel>().encryptData;
            }

            // 重置流的位置，以便模型绑定可以正确工作
            context.Request.Body.Position = 0;

            // 执行解密操作
            var decryptedContent = AESEncryption.AesDecrypt(bodyAsString, KeyVariable.AesKey);

            switch (SpecialAnalyticalRequest(context))
            {
                case true:
                    var keyValuePairs = decryptedContent.ToObject<Dictionary<string, string>>();
                    var decodedForm = new FormUrlEncodedContent(keyValuePairs);

                    // 返回`x-www-form-urlencoded`格式的字符串
                    var decryptedData = await decodedForm.ReadAsStringAsync();
                    var encryptedBytes = Encoding.UTF8.GetBytes(decryptedData);
                    var decryptedStream = new MemoryStream(encryptedBytes);
                    context.Request.Body = decryptedStream;
                    context.Request.ContentLength = encryptedBytes.Length;
                    break;
                default:
                    var bytes = Encoding.UTF8.GetBytes(decryptedContent);

                    // 替换请求体
                    context.Request.Body = new MemoryStream(bytes);
                    context.Request.ContentLength = bytes.Length;
                    break;
            }

            // 暂存原始响应流
            var originalResponseBodyStream = context.Response.Body;

            // 使用 MemoryStream 暂存响应数据
            using (var memoryStream = new MemoryStream())
            {
                // 重定向响应流到内存中
                context.Response.Body = memoryStream;

                // 调用管道中的下一个中间件
                await _next(context);

                // 重置内存流的位置，以便读取
                memoryStream.Position = 0;

                // 读取响应内容
                var responseContent = await new StreamReader(memoryStream).ReadToEndAsync();

                // 执行加密操作
                var encryptedContent = AESEncryption.AesEncrypt(responseContent, KeyVariable.AesKey);

                // 将加密数据写回原始响应流
                var encryptedBytes = Encoding.UTF8.GetBytes(encryptedContent);
                context.Response.ContentType = "text/plain";
                context.Response.Headers.Remove("Content-Length");
                context.Response.Headers.Add("Content-Length", new[] { encryptedBytes.Length.ToString() });
                await originalResponseBodyStream.WriteAsync(encryptedBytes, 0, encryptedBytes.Length);

                // 恢复原始响应流
                context.Response.Body = originalResponseBodyStream;
            }

        }
        finally
        {
            // 恢复原始的请求体，以供后续中间件使用
            context.Request.Body = originalRequestBody;
        }
    }

    private bool SpecialAnalyticalRequest(HttpContext context)
    {
        var filtrationList = new List<string>
        {
            "/api/oauth/Login",
            "/api/file/merge",
            "/api/extend/Document/merge",
        };

        return filtrationList.Contains(context.Request.Path.Value);
    }

    private class encryptModel
    {
        public string encryptData { get; set; }
    }
}


