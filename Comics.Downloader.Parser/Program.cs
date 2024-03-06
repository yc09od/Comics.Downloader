// See https://aka.ms/new-console-template for more information

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text;
using Comics.Downloader.Parser.DataObject;
using Comics.Downloader.Parser.Parser;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using RestSharp;
using Sprache;
using Microsoft.Extensions.Logging;
using Serilog;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Comics.Downloader.Parser;

public class Program
{
    public static void test1()
    {
        Console.WriteLine("Hello, World!");

        var url = "https://www.manhuagui.com/comic/4688/";

        var parser = new ManHuaGuiParser(url);
        var chapters = parser.GetChapters(url);
        var pages = parser.GetPages(chapters.First());

        parser.DownloadImages(pages, "c:\\temp", "bigsword");



        //Console.WriteLine(parser.GetPageCount("https://www.manhuagui.com/comic/4688/40268.html"));
    }


    public async static Task<int> Main(string[] args)
    {
        test1();
        return 0;
    }
}