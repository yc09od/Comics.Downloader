namespace Comics.Downloader.Parser.DataObject;

public class Page
{
    public string Url { get; set; }

    public string label { get; set; }

    public int index { get; set; }

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string FileName { get; set; }

    public string ImageUrl { get; set; }
}