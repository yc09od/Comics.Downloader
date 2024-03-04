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

namespace Comics.Downloader.Parser.Parser
{
    public class ManHuaGuiParser
    {
        private string ChapterPageUrl { get; }
        private const string Host = "https://www.manhuagui.com";
        private const string PageSelectorId = "pageSelect";
        private const int WaitTime = 3000;
        private const int TaskQueueSize = 3;

        public ManHuaGuiParser(string chapterPageUrl)
        {
            this.ChapterPageUrl = chapterPageUrl;
            Log.Logger = new LoggerConfiguration().WriteTo.File("c:\\temp\\Downloader.log").CreateLogger();
        }

        public static Parser<Chapter> ChapterRowParser = from header in Parse.AnyChar.Until(Parse.String("<li><a href=\""))
            from link in Parse.AnyChar.Until(Parse.Char('"')).Text()
            from s in Parse.AnyChar.Until(Parse.String("title=\""))
            from title in Parse.AnyChar.Until(Parse.Char('"')).Text()
            select new Chapter { Label = title, FirstPageUrl = link };

        public static Parser<List<Chapter>> ChapterListParser =
            from w in Parse.AnyChar.Until(Parse.String("id=\"chapter-list-0\""))
            from list in ChapterRowParser.Many()
            from left in Parse.AnyChar.Many().Text()
            select list.ToList();
        
        public List<Chapter> GetChapters(string chapterPageUrl = "")
        {
            chapterPageUrl = string.IsNullOrEmpty(chapterPageUrl) ? this.ChapterPageUrl : chapterPageUrl;
            var chapterPageHtml = GetChapterPageHtml(chapterPageUrl);
            var result = ChapterListParser.Parse(chapterPageHtml).Select((x, i) => new Chapter { Name = i.ToString(), FirstPageUrl = x.FirstPageUrl, Index = i, Label = x.Label}).ToList();
            result.ForEach( x => x.FirstPageUrl = $"{Host}{x.FirstPageUrl}");
            return result;
        }

        public int GetPageCount(string pageUrl)
        {
            Log.Information("GetPageCount start");
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            var result = string.Empty;

            using (var driver = new ChromeDriver(chromeOptions))
            {
                driver.Navigate().GoToUrl(pageUrl);
                var content = driver.PageSource;
                var selector = driver.FindElement(By.Id(PageSelectorId));


                Log.Information("GetPageCount success");
                

                return selector.FindElements(By.TagName("option")).Count;
            }

        }

        public bool DownloadImages(List<Page> pages, string rootPath, string chapterPath)
        {
            try
            {
                Log.Information("DownloadImages start");


                pages.ForEach(x => { DownloadImage(x, rootPath, chapterPath); });
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



        public bool DownloadImage(Page page, string rootPath, string chapterPath)
        {
            try
            {
                Log.Information($"DownloadImages start {page.label} {page.ImageUrl}");
                RestClient restClient = new RestClient(page.ImageUrl);
                restClient.AddDefaultHeader("referer", Host);
                var fileBytes = restClient.DownloadData(new RestRequest("#", Method.Get));
                File.WriteAllBytes(Path.Combine(rootPath, chapterPath, page.FileName), fileBytes);
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

        public List<Page> GetPages(Chapter chapter, int startIndex=0, int count=0)
        {
            Log.Information($"GetPages start {chapter.Label} {ChapterPageUrl}");
            

            var result = new List<Page>();
            var pageCount = GetPageCount(chapter.FirstPageUrl);
            count = count == 0 ? pageCount - startIndex : count;
            result = Enumerable.Range(0, pageCount).Select((v, i) => new Page
                { index = i, label = (i + 1).ToString(), FileName = $"{i}.jpg", Url = $"{chapter.FirstPageUrl}#p={(i + 1).ToString()}" }).ToList();

            result = result.GetRange(startIndex, count);

            ConcurrentBag<KeyValuePair<string, string>>
                urlContainer = new ConcurrentBag<KeyValuePair<string, string>>();

            var tasks = result.Select(x => new Task(() =>
            {
                var imageUrl = GetPageImageUrl(x.Url);
                urlContainer.Add(new KeyValuePair<string, string>(x.Id, imageUrl));
            })).ToArray();

            while (tasks.Any(x => x.Status == TaskStatus.Created))
            {
                var pauseTasks = tasks.Where(x => x.Status == TaskStatus.Created || x.Status == TaskStatus.Faulted);
                var selectedPauseTasks = pauseTasks.Where((v, i) => i < TaskQueueSize).ToArray();
                foreach (var task in selectedPauseTasks)
                {
                    task.Start();
                }

                try
                {
                    Log.Information($"GetPageImageUrl tasks start");

                    Task.WhenAll(selectedPauseTasks).GetAwaiter().GetResult();

                    Log.Information($"GetPageImageUrl tasks success");
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Log.Error($"GetPageImageUrl tasks fail {e.Message}");
                    }
                }
                catch (Exception e)
                {
                    //ignore
                }
            }
            
            result.ForEach(x => x.ImageUrl = urlContainer.SingleOrDefault(y => y.Key.Equals(x.Id)).Value);

            Log.Information($"GetPages success {chapter.Label} {ChapterPageUrl}");
            

            return result;
        }

        public string GetPageImageUrl(string pageUrl)
        {
            Log.Information($"GetPageImageUrl tasks start {pageUrl}");
            
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            var result = string.Empty;

            using (var driver = new ChromeDriver(chromeOptions))
            {
                try
                {
                    driver.Navigate().GoToUrl(pageUrl);
                    System.Threading.Thread.Sleep(WaitTime);
                    try
                    {
                        var button = driver.FindElement(By.Id("checkAdult"));
                        button.Click();
                    }
                    catch
                    {
                        //ignore
                    }

                    IWebElement image;

                    // 等待一段时间，确保按钮点击后页面加载完成
                    System.Threading.Thread.Sleep(WaitTime);
                    try
                    {
                        image = driver.FindElement(By.Id("mangaFile"));
                    }
                    catch
                    {
                        throw;
                    }
                    
                    // 获取页面内容
                    result = image.GetAttribute("src");

                    Log.Information($"GetPageImageUrl tasks success {pageUrl}");
                    return result;
                }
                catch (Exception e)
                {
                    File.WriteAllText("c:\\temp\\error.txt", driver.PageSource);
                    Log.Error($"GetPageImageUrl tasks fail {pageUrl}");
                    throw;
                }
                finally
                {
                    
                    driver.Quit();
                }
            }
        }

        public string GetChapterPageHtml(string chapterPageUrl)
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
                    driver.Navigate().GoToUrl(chapterPageUrl);

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
                    

                    // 等待一段时间，确保按钮点击后页面加载完成
                    System.Threading.Thread.Sleep(WaitTime);

                    // 获取页面内容
                    string pageContent = driver.PageSource;
                    result = pageContent;
                }
                catch (Exception e)
                {
                    Log.Error($"GetChapterPageHtml fail {chapterPageUrl}");
                    throw;
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
