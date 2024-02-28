using System.Collections.Concurrent;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using Sprache;
using Comics.Downloader.Parser.DataObject;
using RestSharp;

namespace Comics.Downloader.Parser.Parser
{
    public class ManHuaGuiParser
    {
        private string ChapterPageUrl { get; }
        private const string Host = "https://www.manhuagui.com";
        private const string PageSelectorId = "pageSelect";

        public ManHuaGuiParser(string chapterPageUrl)
        {
            this.ChapterPageUrl = chapterPageUrl;
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
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            var result = string.Empty;

            using (var driver = new ChromeDriver(chromeOptions))
            {
                driver.Navigate().GoToUrl(pageUrl);
                var content = driver.PageSource;
                var selector = driver.FindElement(By.Id(PageSelectorId));
                return selector.FindElements(By.TagName("option")).Count;
            }
        }

        public bool DownloadImages(List<Page> pages, string rootPath, string chapterPath)
        {
            try
            {
                pages.ForEach(x =>
                {
                    DownloadImage(x, rootPath, chapterPath);
                });
            }
            catch (Exception e)
            {

                return false;
            }

            return true;
        }



        public bool DownloadImage(Page page, string rootPath, string chapterPath)
        {
            try
            {
                RestClient restClient = new RestClient(page.ImageUrl);
                restClient.AddDefaultHeader("referer", Host);
                var fileBytes = restClient.DownloadData(new RestRequest("#", Method.Get));
                File.WriteAllBytes(Path.Combine(rootPath, chapterPath, page.FileName), fileBytes);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public List<Page> GetPages(Chapter chapter, int startIndex, int count)
        {
            var result = new List<Page>();
            var pageCount = GetPageCount(chapter.FirstPageUrl);
            result = Enumerable.Range(0, pageCount).Select((v, i) => new Page
                { index = i, label = (i + 1).ToString(), FileName = $"{i}.jpg", Url = $"{chapter.FirstPageUrl}#p={(i + 1).ToString()}" }).ToList();

            result = result.GetRange(startIndex, count);

            ConcurrentBag<KeyValuePair<string, string>>
                urlContainer = new ConcurrentBag<KeyValuePair<string, string>>();

            var tasks = result.Select(x => new Task(() =>
            {
                Console.WriteLine($"=====> task {x.label} || {x.Url}");
                var imageUrl = GetPageImageUrl(x.Url);
                urlContainer.Add(new KeyValuePair<string, string>(x.Id, imageUrl));
            })).ToArray();

            while (tasks.Any(x => x.Status == TaskStatus.Created))
            {
                var pauseTasks = tasks.Where(x => x.Status == TaskStatus.Created);
                var selectedPauseTasks = pauseTasks.Where((v, i) => i < 3).ToArray();
                foreach (var task in selectedPauseTasks)
                {
                    task.Start();
                }
                
                Task.WaitAll(selectedPauseTasks);
            }
            
            result.ForEach(x => x.ImageUrl = urlContainer.SingleOrDefault(y => y.Key.Equals(x.Id)).Value);

            return result;
        }

        public string GetPageImageUrl(string pageUrl)
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            var result = string.Empty;

            using (var driver = new ChromeDriver(chromeOptions))
            {
                // 导航到目标页面
                driver.Navigate().GoToUrl(pageUrl);

                // 找到并点击按钮
                try
                {
                    var button = driver.FindElement(By.Id("checkAdult"));
                    button.Click();

                    // 等待一段时间，确保按钮点击后页面加载完成
                    System.Threading.Thread.Sleep(5000);
                }
                catch
                {
                    //
                }

                var image = driver.FindElement(By.Id("mangaFile"));
                // 获取页面内容
                result = image.GetAttribute("src");

                // 关闭浏览器
                driver.Quit();
            }

            return result;
        }

        public string GetChapterPageHtml(string chapterPageUrl)
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            var result = string.Empty;

            using (var driver = new ChromeDriver(chromeOptions))
            {
                // 导航到目标页面
                driver.Navigate().GoToUrl(chapterPageUrl);

                // 找到并点击按钮
                var button = driver.FindElement(By.Id("checkAdult"));
                button.Click();

                // 等待一段时间，确保按钮点击后页面加载完成
                System.Threading.Thread.Sleep(5000);

                // 获取页面内容
                string pageContent = driver.PageSource;
                result = pageContent;

                // 关闭浏览器
                driver.Quit();
            }

            return result;
        }
    }
}
