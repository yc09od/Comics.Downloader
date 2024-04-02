// See https://aka.ms/new-console-template for more information

using Comics.Downloader.Parser.Parser;

namespace Comics.Downloader.Parser;

public class Program
{
    public static async Task test1()
    {
        Console.WriteLine("Hello, World!");

        var url = "https://www.manhuagui.com/comic/4688/";

        var parser = new ManHuaGuiParser(url);
        var chapters = await parser.GetChapters(url);
        var pages = await parser.GetPages(chapters[1], 0, 100);

        await parser.DownloadImages(pages, "c:\\temp", "bigsword").ConfigureAwait(false);

        //Console.WriteLine(parser.GetPageCount("https://www.manhuagui.com/comic/4688/40268.html"));
    }


    public async static Task<int> Main(string[] args)
    {
        await test1();
        return 0;
    }
}