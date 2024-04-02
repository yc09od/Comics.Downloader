using System.Collections.Concurrent;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using Sprache;
using Comics.Downloader.Parser.DataObject;
using RestSharp;
using Serilog;
using Serilog.Core;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Comics.Downloader.Parser.Parser
{
    public class ManHuaGuiParser
    {
        private string ChapterPageUrl { get; }
        private const string Host = "https://www.manhuagui.com";
        private const string PageSelectorId = "pageSelect";
        private const int WaitTime = 3000;
        private const int TaskQueueSize = 10;

        public ManHuaGuiParser(string chapterPageUrl)
        {
            this.ChapterPageUrl = chapterPageUrl;
            Log.Logger = new LoggerConfiguration().WriteTo.File("c:\\temp\\Downloader.log").CreateLogger();
        }

        public static Parser<Chapter> ChapterRowParser =
            from header in Parse.AnyChar.Until(Parse.String("<li><a href=\""))
            from link in Parse.AnyChar.Until(Parse.Char('"')).Text()
            from s in Parse.AnyChar.Until(Parse.String("title=\""))
            from title in Parse.AnyChar.Until(Parse.Char('"')).Text()
            select new Chapter { Label = title, FirstPageUrl = link };

        public static Parser<List<Chapter>> ChapterListParser =
            from w in Parse.AnyChar.Until(Parse.String("id=\"chapter-list-0\""))
            from list in ChapterRowParser.Many()
            from left in Parse.AnyChar.Many().Text()
            select list.ToList();

        public async Task<List<Chapter>> GetChapters(string chapterPageUrl = "")
        {
            chapterPageUrl = string.IsNullOrEmpty(chapterPageUrl) ? this.ChapterPageUrl : chapterPageUrl;
            var chapterPageHtml = await GetChapterPageHtml(chapterPageUrl);
            var result = ChapterListParser.Parse(chapterPageHtml).Select((x, i) => new Chapter
                { Name = i.ToString(), FirstPageUrl = x.FirstPageUrl, Index = i, Label = x.Label }).ToList();
            result.ForEach(x => x.FirstPageUrl = $"{Host}{x.FirstPageUrl}");
            return result;
        }

        public async Task<int> GetPageCount(string pageUrl)
        {
            Log.Information("GetPageCount start");
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            int result;

            using (var driver = new ChromeDriver(chromeOptions))
            {
                await Task.Run(() => driver.Navigate().GoToUrl(pageUrl));
                //var content = driver.PageSource;
                var selector = driver.FindElement(By.Id(PageSelectorId));

                var resultTask = Task.Run(() =>
                {
                    var selector = driver.FindElement(By.Id(PageSelectorId));
                    return selector.FindElements(By.TagName("option")).Count;
                });

                Log.Information("GetPageCount success");

                result = await resultTask;

            }

            return result;
        }

        public async Task<bool> DownloadImages(List<Page> pages, string rootPath, string chapterPath)
        {
            try
            {
                Log.Information("DownloadImages start");
                var queue = pages.Select(x => DownloadImage(x, rootPath, chapterPath));

                await Task.WhenAll(queue);
            }
            catch (Exception e)
            {
                Log.Error($"DownloadImages fails {e.Message}");
                return false;
            }
            finally
            {
            }

            Log.Information("DownloadImages start");


            return true;
        }


        public async Task<bool> DownloadImage(Page page, string rootPath, string chapterPath)
        {
            try
            {
                Log.Information($"DownloadImages start {page.label} {page.ImageUrl}");
                RestClient restClient = new RestClient(page.ImageUrl);
                restClient.AddDefaultHeader("referer", Host);
                var fileBytes = await restClient.DownloadDataAsync(new RestRequest("#", Method.Get));
                await File.WriteAllBytesAsync(Path.Combine(rootPath, chapterPath, page.FileName), fileBytes);
            }
            catch (Exception e)
            {
                Log.Error($"DownloadImages fail {page.label} {page.ImageUrl} {e.Message}");
                return false;
            }
            finally
            {
            }

            Log.Information($"DownloadImages success {page.label} {page.ImageUrl}");


            return true;
        }

        public async Task<List<Page>> GetPages(Chapter chapter, int startIndex = 0, int count = 0)
        {
            Log.Information($"GetPages start {chapter.Label} {ChapterPageUrl}");


            var result = new List<Page>();
            var pageCount = await GetPageCount(chapter.FirstPageUrl);
            count = count == 0 ? pageCount - startIndex : count;
            result = Enumerable.Range(0, pageCount).Select((v, i) => new Page
            {
                index = i, label = (i + 1).ToString(), FileName = $"{i}.jpg",
                Url = $"{chapter.FirstPageUrl}#p={(i + 1).ToString()}"
            }).ToList();

            result = result.GetRange(startIndex, count);

            ConcurrentBag<KeyValuePair<string, string>>
                urlContainer = new ConcurrentBag<KeyValuePair<string, string>>();

            async Task<int> CreateTask(Page page)
            {

                var imageUrl = GetPageImageUrl(page.Url);
                urlContainer.Add(new KeyValuePair<string, string>(page.Id, await imageUrl));
                return 0;
            };

            var tasks = result.Select(x => new KeyValuePair<Page, Task<int>?>(x, null)).ToList();

            while (tasks.Any(x => x.Value == null || !x.Value.IsCompleted))
            {
                var pauseTasks = tasks.Where(x => x.Value is null).ToList();
                var selectedPauseTasks = pauseTasks.GetRange(0, Math.Min(TaskQueueSize, pauseTasks.Count)).ToList();

                Log.Information(
                    $"GetPageImageUrl tasks start {string.Join("\t|\t", selectedPauseTasks.Select(x => x.Key.label))}");

                for (var i = 0; i < selectedPauseTasks.Count; i++)
                {
                    var pair = selectedPauseTasks[i];

                    if (pair.Value is null)
                    {
                        selectedPauseTasks[i] = new KeyValuePair<Page, Task<int>?>(pair.Key, CreateTask(pair.Key));
                        tasks[tasks.IndexOf(pair)] = selectedPauseTasks[i];
                    }
                }

                try
                {
                    await Task.WhenAll(selectedPauseTasks.Select(x => x.Value)!);

                    Log.Information($"GetPageImageUrl tasks success");
                }
                catch (Exception e)
                {
                    var errorPagesTasks = selectedPauseTasks.Where(x => x.Value.IsFaulted)
                        .Select(x => new KeyValuePair<Page, Task<int>?>(x.Key, null));
                    tasks = tasks.Concat(errorPagesTasks).ToList();
                    Log.Error($"GetPageImageUrl tasks fail {e.Message} and ### in exception");
                }
            }

            result.ForEach(x => x.ImageUrl = urlContainer.SingleOrDefault(y => y.Key.Equals(x.Id)).Value);

            Log.Information($"GetPages success {chapter.Label} {ChapterPageUrl}");


            return result;
        }

        public async Task<string> GetPageImageUrl(string pageUrl)
        {
            Log.Information($"GetPageImageUrl start {pageUrl}");

            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            var result = string.Empty;


            using (var driver = new ChromeDriver(chromeOptions))
            {
                try
                {
                    await Task.Run(() => { driver.Navigate().GoToUrl(pageUrl); });

                    await Task.Delay(WaitTime);

                    await Task.Run(() =>
                    {
                        try
                        {
                            var button = driver.FindElement(By.Id("checkAdult"));
                            button.Click();
                        }
                        catch
                        {
                            //ignore
                        }
                    });

                    IWebElement image;

                    // 等待一段时间，确保按钮点击后页面加载完成
                    await Task.Delay(WaitTime);
                    image = await Task.Run(() =>
                    {
                        image = driver.FindElement(By.Id("mangaFile"));
                        return image;
                    });

                    // 获取页面内容
                    result = image.GetAttribute("src");

                    Log.Information($"GetPageImageUrl success {pageUrl}");
                    return result;
                }
                catch (Exception e)
                {
                    File.WriteAllText("c:\\temp\\error.txt", driver.PageSource);
                    Log.Error($"GetPageImageUrl fail {pageUrl}");
                    throw new AggregateException(e);
                }
                finally
                {
                    driver.Quit();
                }
            }
        }

        public async Task<string> GetChapterPageHtml(string chapterPageUrl)
        {
            Log.Information($"GetChapterPageHtml start {chapterPageUrl}");

            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            var result = string.Empty;

            using (var driver = new ChromeDriver(chromeOptions))
            {
                try
                {
                    // 导航到目标页面
                    await Task.Run(() =>
                    {
                        driver.Navigate().GoToUrl(chapterPageUrl);
                    });

                    await Task.Run(() =>
                    {
                        try
                        {
                            // 找到并点击按钮
                            var button = driver.FindElement(By.Id("checkAdult"));
                            button.Click();
                        }
                        catch
                        {
                            //ignore
                        }
                    });

                    // 等待一段时间，确保按钮点击后页面加载完成
                    await Task.Delay(WaitTime);

                    // 获取页面内容
                    result = driver.PageSource;
                }
                catch (Exception ex)
                {
                    Log.Error($"GetChapterPageHtml fail {chapterPageUrl}");
                    throw ex;
                }
                finally
                {
                    // 关闭浏览器
                    driver.Quit();
                }
            }

            Log.Information($"GetChapterPageHtml success {chapterPageUrl}");


            return result;
        }
    }
}