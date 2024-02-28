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

namespace Comics.Downloader.Parser;

public class Program
{
    public static void test1()
    {
        Console.WriteLine("Hello, World!");

        var url = "https://www.manhuagui.com/comic/4688/";

        var parser = new ManHuaGuiParser(url);
        var chapters = parser.GetChapters(url);
        var pages = parser.GetPages(chapters.First(), 0, 10);

        chapters.ForEach(x =>
        {
            //Console.WriteLine($"url = {x.FirstPageUrl}, label={x.label}");
        });

        StringBuilder sb = new StringBuilder();

        pages.ForEach(x =>
        {
            sb.Append($"url = {x.Url}, label={x.label}, imageUrl={x.ImageUrl}\n");
        });

        parser.DownloadImages(pages, "c:\\temp", "bigsword");



        //Console.WriteLine(parser.GetPageCount("https://www.manhuagui.com/comic/4688/40268.html"));
        Console.ReadLine();
    }


    public static void Main(string[] args)
    {

        test1();

        
        
    }
}