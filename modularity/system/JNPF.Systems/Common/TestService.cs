using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.JsonSerialization;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Permission;
using JNPF.UnifyResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SqlSugar;
using System.Drawing;
using System.Drawing.Imaging;
using Screenshot = OpenQA.Selenium.Screenshot;

namespace JNPF.Systems.Common;

/// <summary>
/// 测试接口.
/// </summary>
[ApiDescriptionSettings(Name = "Test", Order = 306)]
[Route("api")]
public class TestService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<UserEntity> _sqlSugarRepository;
    private readonly IDataBaseManager _databaseService;
    private readonly ITenant _db;
    private readonly IFileManager _fileManager;

    public TestService(ISqlSugarRepository<UserEntity> sqlSugarRepository, ISqlSugarClient context, IDataBaseManager databaseService, IFileManager fileManager)
    {
        _sqlSugarRepository = sqlSugarRepository;
        _databaseService = databaseService;
        _fileManager = fileManager;
        _db = context.AsTenant();
    }

    [HttpGet("test")]
    [AllowAnonymous]
    public async Task<dynamic> test()
    {
        var list = new List<object>();
        var dic = new Dictionary<string, object>();
        dic["handleId"] = "03d159a3-0f88-424c-a24f-02f63855fe4f";
        list.Add(dic);
        var sss = dic["32"].ToString();
        return list;
        //GetWebContent();
        //try
        //{

        //}
        //catch (Exception ex)
        //{
        //    throw Oops.Oh(ErrorCode.COM1000,ex.Message);
        //}
    }

    [HttpPost("test/outside")]
    [AllowAnonymous]
    public async Task testoutside([FromBody] OutSide input)
    {
        var response = new RESTfulResult<object>();
        var dicHerader = new Dictionary<string, object>();
        //if (KeyVariable.MultiTenancy && !string.IsNullOrEmpty(tenantId))
        //{
        //    await _tenantManager.ChangTenant((SqlSugarScope)_sqlSugarClient, tenantId);
        //}
        var user = await _sqlSugarRepository.GetFirstAsync(it => it.Id.Equals("03d159a3-0f88-424c-a24f-02f63855fe4f"));

        //// 生成实时token
        var toKen = NetHelper.GetToken(user.Id, user.Account, user.RealName, user.IsAdministrator, user.TenantId);
        dicHerader.Add("Authorization", toKen);
        dicHerader.Add("jnpf-origin", "pc");
        var localAddress = "http://localhost:5000";
        var path = string.Format("{0}{1}", localAddress, "/api/workflow/Task/Hooks");
        var parameter = new Dictionary<string, object>();
        parameter.Add("data", input.formData);
        parameter.Add("nodeId", input.nodeId);
        response = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(parameter).PostAsStringAsync()).ToObject<RESTfulResult<object>>();
    }

    [HttpGet("test/getoutside")]
    [AllowAnonymous]
    public async Task getoutside([FromQuery] OutSide input)
    {
        var response = new RESTfulResult<object>();
        var dicHerader = new Dictionary<string, object>();
        //if (KeyVariable.MultiTenancy && !string.IsNullOrEmpty(tenantId))
        //{
        //    await _tenantManager.ChangTenant((SqlSugarScope)_sqlSugarClient, tenantId);
        //}
        var user = await _sqlSugarRepository.GetFirstAsync(it => it.Id.Equals("03d159a3-0f88-424c-a24f-02f63855fe4f"));

        //// 生成实时token
        var toKen = NetHelper.GetToken(user.Id, user.Account, user.RealName, user.IsAdministrator, user.TenantId);
        dicHerader.Add("Authorization", toKen);
        dicHerader.Add("jnpf-origin", "pc");
        var localAddress = "http://localhost:5000";
        var path = string.Format("{0}{1}", localAddress, "/api/workflow/Task/Hooks");
        var parameter = new Dictionary<string, object>();
        var data = new { id = input.id };
        parameter.Add("data", data);
        parameter.Add("flowId", input.flowId);
        parameter.Add("taskId", input.taskId);
        parameter.Add("nodeId", input.nodeId);
        response = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(parameter).PostAsStringAsync()).ToObject<RESTfulResult<object>>();
    }

    public void GetWebContent()
    {
        var chromeOptions = new ChromeOptions();
        chromeOptions.AddArgument("--headless");
        IWebDriver driver = new ChromeDriver(chromeOptions);
        try
        {
            // 打开你想要截图的网页
            driver.Navigate().GoToUrl("https://gitee.com/gvp/all");
            driver.Manage().Window.Maximize();
            // 等待元素可见
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            IWebElement element = driver.FindElement(By.ClassName("gvp-category-container"));
            var bitmap = GetElementScreenshot(driver, element);
            // 保存截图到文件
            string savePath = "C:\\Users\\JNPF\\Desktop\\工作文件\\screenshot.png"; // 指定截图保存的路径和文件名
            bitmap.Save(savePath, ImageFormat.Png);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            // 关闭浏览器
            driver.Quit();
        }
    }

    private static Bitmap GetElementScreenshot(IWebDriver driver, IWebElement element)
    {
        try
        {
            int totalHeight = ((IJavaScriptExecutor)driver).ExecuteScript("return Math.max(document.body.scrollHeight, document.body.offsetHeight, document.documentElement.clientHeight, document.documentElement.scrollHeight, document.documentElement.offsetHeight);").ParseToInt();
            int viewportHeight = driver.Manage().Window.Size.Height;
            Bitmap bitmap = new Bitmap(driver.Manage().Window.Size.Width, totalHeight);
            Graphics fullPageScreenshotGraphics = Graphics.FromImage(bitmap);

            int yOffset = 0;
            while (yOffset < totalHeight)
            {
                // 滚动到指定位置
                ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollTo(0, " + yOffset + ");");
                Thread.Sleep(500); // 等待页面加载完成

                // 截取当前可视区域的截图
                Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                Bitmap image = new Bitmap(new MemoryStream(screenshot.AsByteArray));

                // 将截图绘制到全页截图中
                fullPageScreenshotGraphics.DrawImage(image, 0, yOffset);

                // 计算下一个滚动位置
                yOffset += viewportHeight;
            }
            return bitmap;
        }
        catch (Exception)
        {
            throw;
        }
    }













    public void xx32323232()
    {
        // 初始化Chrome驱动
        var chromeOptions = new ChromeOptions();
        IWebDriver driver = new ChromeDriver(chromeOptions);
        try
        {
            // 打开网页
            driver.Navigate().GoToUrl("https://gitee.com/gvp/all");

            // 定位到你想要截图的元素
            IWebElement element = driver.FindElement(By.ClassName("gvp-category-container")); // 替换为你的元素ID

            // 滚动到元素以确保它在视口中
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);

            // 使用JavaScript获取元素的截图
            string script = @"
                var element = arguments[0];
                var canvas = document.createElement('canvas');
                var ctx = canvas.getContext('2d');
                var rect = element.getBoundingClientRect();
                canvas.width = rect.width;
                canvas.height = rect.height;
                ctx.drawImage(element, 0, 0, rect.width, rect.height);
                return canvas.toDataURL('image/png').replace('image/png', 'image/octet-stream');
            ";

            string base64Image = ((IJavaScriptExecutor)driver).ExecuteScript(script, element) as string;

            // 将Base64编码的图片转换为字节数组
            byte[] imageBytes = Convert.FromBase64String(base64Image.Split(',')[1]);

            // 将字节数组转换为图片
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                Image image = Image.FromStream(ms);

                // 保存截图到文件
                string screenshotPath = "elementScreenshot.png";
                image.Save(screenshotPath, ImageFormat.Png);
                Console.WriteLine("Element screenshot saved as " + screenshotPath);
            }
        }
        catch (Exception)
        {

        }
        finally
        {
            // 关闭浏览器
            driver.Quit();
        }
    }



    public void xx3232()
    {
        // 初始化Chrome驱动
        var chromeDriverService = ChromeDriverService.CreateDefaultService();
        var chromeOptions = new ChromeOptions();
        //chromeOptions.AddArguments(new string[] { "--headless" });
        IWebDriver driver = new ChromeDriver(chromeDriverService, chromeOptions);

        try
        {
            // 打开网页
            driver.Navigate().GoToUrl("https://gitee.com/gvp/all");
            driver.Manage().Window.Maximize();
            // 定位到你想要截图的元素
            IWebElement element = driver.FindElement(By.ClassName("gvp-category-container")); // 替换为你要截图的元素的ID

            // 确保元素完全可见（如果元素超出视口，滚动到元素底部）
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(false);", element);

            // 获取元素的截图（如果元素大小小于视口大小）
            if (element.Size.Height <= driver.Manage().Window.Size.Height)
            {
                ITakesScreenshot screenshotTaker = element as ITakesScreenshot;
                Screenshot screenshot = screenshotTaker.GetScreenshot();

                // 裁剪出元素所在的区域
                Point elementLocation = element.Location;
                Size elementSize = element.Size;
                Rectangle cropRect = new Rectangle(elementLocation.X, elementLocation.Y, elementSize.Width, elementSize.Height);
                Bitmap sourceImage = new Bitmap(new MemoryStream(screenshot.AsByteArray));
                Bitmap croppedImage = sourceImage.Clone(cropRect, sourceImage.PixelFormat);

                // 保存截图到文件
                string screenshotPath = "C:\\Users\\JNPF\\Desktop\\工作文件\\screenshot.png"; // 指定截图保存的路径和文件名
                croppedImage.Save(screenshotPath, ImageFormat.Png);
            }
            else
            {
                //var result = ((IJavaScriptExecutor)driver).ExecuteScript("return {'w':Math.max(document.body.scrollWidth, document.body.offsetWidth, document.documentElement.clientWidth, document.documentElement.scrollWidth, document.documentElement.offsetWidth), 'h':Math.max(document.body.scrollHeight, document.body.offsetHeight, document.documentElement.clientHeight, document.documentElement.scrollHeight, document.documentElement.offsetHeight)};");
                //var dic = result.ToObject<Dictionary<string, int>>();
                //var h = dic["h"];
                //var w = dic["w"];
                //Bitmap fullPageScreenshot = new Bitmap(w, h);

                //Screenshot screenshot = ((ITakesScreenshot)element).GetScreenshot();
                //Point elementLocation = element.Location;
                //Size elementSize = element.Size;
                //Rectangle cropRect = new Rectangle(elementLocation.X, elementLocation.Y, elementSize.Width, elementSize.Height);
                //Bitmap sourceImage = new Bitmap(new MemoryStream(screenshot.AsByteArray));
                //Bitmap croppedImage = sourceImage.Clone(cropRect, sourceImage.PixelFormat);
                //Bitmap fullPageScreenshot = new Bitmap(w, totalHeight);

                // 如果元素超过视口大小，则需要特殊处理，比如分段截图并拼接
                int totalHeight = ((IJavaScriptExecutor)driver).ExecuteScript("return Math.max(document.body.scrollHeight, document.body.offsetHeight, document.documentElement.clientHeight, document.documentElement.scrollHeight, document.documentElement.offsetHeight);").ParseToInt();
                int viewportHeight = driver.Manage().Window.Size.Height;


                Bitmap fullPageScreenshot = new Bitmap(driver.Manage().Window.Size.Width, totalHeight);
                Graphics fullPageScreenshotGraphics = Graphics.FromImage(fullPageScreenshot);

                int yOffset = 0;
                while (yOffset < totalHeight)
                {
                    // 滚动到指定位置
                    ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollTo(0, " + yOffset + ");");
                    Thread.Sleep(500); // 等待页面加载完成

                    // 截取当前可视区域的截图
                    Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                    Bitmap image = new Bitmap(new MemoryStream(screenshot.AsByteArray));

                    // 将截图绘制到全页截图中
                    fullPageScreenshotGraphics.DrawImage(image, 0, yOffset);

                    // 计算下一个滚动位置
                    yOffset += viewportHeight;
                }

                // 裁剪出元素所在的区域
                Point elementLocation = element.Location;
                Size elementSize = element.Size;
                Rectangle cropRect = new Rectangle(elementLocation.X, elementLocation.Y, elementSize.Width, elementSize.Height);
                Bitmap croppedImage = fullPageScreenshot.Clone(cropRect, fullPageScreenshot.PixelFormat);

                // 保存截图到文件
                string screenshotPath = "C:\\Users\\JNPF\\Desktop\\工作文件\\screenshot.png"; // 指定截图保存的路径和文件名
                croppedImage.Save(screenshotPath, ImageFormat.Png);
            }
        }
        catch (Exception)
        {

        }
        finally
        {
            // 关闭浏览器
            driver.Quit();
        }
    }

    public void xx()
    {
        // 初始化ChromeDriver（或其他浏览器的WebDriver）
        IWebDriver driver = new ChromeDriver(); // 确保ChromeDriver的exe文件在你的系统PATH中，或者指定其路径

        try
        {
            // 打开你想要截图的网页
            driver.Navigate().GoToUrl("https://gitee.com/gvp/all");

            // 等待元素可见
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            var element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.ClassName("gvp-category-container"))); // 替换为你的元素ID或定位器

            // 获取整个页面的截图
            ITakesScreenshot screenshotDriver = (ITakesScreenshot)driver;
            OpenQA.Selenium.Screenshot screenshot = screenshotDriver.GetScreenshot();

            // 将截图转换为Bitmap
            Bitmap sourceImage = new Bitmap(new MemoryStream(screenshot.AsByteArray));

            // 获取元素的位置和大小
            Rectangle elementRect = new Rectangle(element.Location.X, element.Location.Y, element.Size.Width, element.Size.Height);

            // 确保截图区域在屏幕截图范围内
            Rectangle bounds = new Rectangle(0, 0, sourceImage.Width, sourceImage.Height);
            elementRect = Rectangle.Intersect(elementRect, bounds);

            // 从整个页面的截图中裁剪出元素的截图
            Bitmap croppedImage = sourceImage.Clone(elementRect, sourceImage.PixelFormat);

            // 保存截图到文件
            string screenshotPath = "C:\\Users\\JNPF\\Desktop\\工作文件\\screenshot.png"; // 指定截图保存的路径和文件名
            croppedImage.Save(screenshotPath, ImageFormat.Png);

            Console.WriteLine("Element screenshot saved as " + screenshotPath);
        }
        finally
        {
            // 关闭浏览器并清理资源
            driver.Quit();
        }
    }

    public void xx1()
    {
        // 初始化ChromeDriver（或其他浏览器的WebDriver）
        IWebDriver driver = new ChromeDriver(); // 确保ChromeDriver的exe文件在你的系统PATH中，或者指定其路径

        try
        {
            // 打开你想要截图的网页
            driver.Navigate().GoToUrl("https://gitee.com/gvp/all");

            // 设置等待时间
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            // 等待元素可见
            var element = wait.Until(d => d.FindElement(By.ClassName("gvp-category-container"))); // 替换为你的元素ID或其他定位器

            // 确保元素在视口中，以便截图完整
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);

            // 获取元素的截图（需要自定义方法，因为Selenium WebDriver本身不支持直接截取元素）
            Bitmap elementScreenshot = GetElementScreenshot(driver, element);

            // 保存截图到文件
            string screenshotPath = "C:\\Users\\JNPF\\Desktop\\工作文件\\screenshot123.png";
            elementScreenshot.Save(screenshotPath, ImageFormat.Png);
            Console.WriteLine($"Element screenshot saved as {screenshotPath}");
        }
        finally
        {
            // 关闭浏览器并清理资源
            driver.Quit();
        }

    }



    public Dictionary<string, object> cs1(Content content)
    {
        var list = content.EnCode.Split(".").ToList();
        list.Reverse();
        var resultDic = new Dictionary<string, object>();
        int index = 0;
        foreach (var item in list)
        {
            var key = item;
            resultDic = index == 0 ? new Dictionary<string, object> { { key, content.Name } } : new Dictionary<string, object> { { key, resultDic } };
            ++index;
        }
        return resultDic;
    }

    public Dictionary<string, object> cs2(Dictionary<string, object> zhDic, Dictionary<string, object> dqDic)
    {
        if (zhDic.Any())
        {
            if (zhDic.ContainsKey(dqDic.Keys.FirstOrDefault()))
            {
                // 最后结果是字符串替换
                if (zhDic[dqDic.Keys.FirstOrDefault()] is string || dqDic[dqDic.Keys.FirstOrDefault()] is string)
                {
                    zhDic[dqDic.Keys.FirstOrDefault()] = dqDic[dqDic.Keys.FirstOrDefault()];
                }
                else
                {
                    var dic1 = zhDic[dqDic.Keys.FirstOrDefault()].ToObject<Dictionary<string, object>>();
                    var dic2 = dqDic[dqDic.Keys.FirstOrDefault()].ToObject<Dictionary<string, object>>();
                    zhDic[dqDic.Keys.FirstOrDefault()] = cs2(dic1, dic2);
                }
            }
            else
            {
                zhDic[dqDic.Keys.FirstOrDefault()] = dqDic[dqDic.Keys.FirstOrDefault()];
            }
        }
        else
        {
            zhDic = dqDic;
        }
        return zhDic;
    }

}


public class TestModel
{
    [JsonConverter(typeof(NewtonsoftJsonDateTimeJsonConverter))]
    public DateTime? firstLogTime { get; set; }
}


public class Content
{
    public string EnCode { get; set; }

    public string Name { get; set; }
}


public class OutSide
{
    public string flowId { get; set; }

    public string taskId { get; set; }

    public string nodeId { get; set; }

    public string id { get; set; }

    public object formData { get; set; }
}