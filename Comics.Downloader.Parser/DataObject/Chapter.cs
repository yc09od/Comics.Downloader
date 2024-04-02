namespace Comics.Downloader.Parser.DataObject;

public class Chapter
{
    public string FirstPageUrl { get; set; }

    public string Label { get; set; }

    public string Name { get; set; }

    public int Index { get; set; }

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public List<Page> Pages { get; set; }
}