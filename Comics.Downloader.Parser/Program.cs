// See https://aka.ms/new-console-template for more information

using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using RestSharp;
using Sprache;

public class Chapter
{
    public string Url { get; set; }

    public string label { get; set; }
}

public class Program
{
    static List<Chapter> GetList(string htmlContent)
    {
        var parserOneRow = from header in Parse.AnyChar.Until(Parse.String("<li><a href=\""))
            from link in Parse.AnyChar.Until(Parse.Char('"')).Text()
            from s in Parse.AnyChar.Until(Parse.String("title=\""))
            from title in Parse.AnyChar.Until(Parse.Char('"')).Text()
            select new Chapter { label = title, Url = link };

        var parserMany = 
            from w in Parse.AnyChar.Until(Parse.String("id=\"chapter-list-0\""))
            from list in parserOneRow.Many()
            from left in Parse.AnyChar.Many().Text()
            select new KeyValuePair<List<Chapter>, string>(list.ToList(), left);


        var result = parserMany.Parse(htmlContent).Key;

        return result;
    }

    static string GetHtml(string url)
    {
        var driverPath = @"C:\OS\chromedriver.exe";
        var chromeOptions = new ChromeOptions();
        chromeOptions.AddArgument("--headless");
        var result = string.Empty;

        using (var driver = new ChromeDriver(chromeOptions))
        {
            // 导航到目标页面
            driver.Navigate().GoToUrl(url);

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

    public static void Main(string[] args)
    {

        Console.WriteLine("Hello, World!");

        var url = "https://www.manhuagui.com/comic/4688/";
        var content = string.Empty;
        // content = GetHtml(url);
        // using (var sw = File.CreateText("c:\\temp\\output.txt"))
        // {
        //     sw.WriteLine(GetHtml(url));
        // }
        content = File.ReadAllText("c:\\temp\\output.txt");

        var test = GetList(content);

        test.ForEach(x =>
        {
            Console.WriteLine($"url = {x.Url}, label={x.label}");
        });

        Console.ReadLine();
    }
}