namespace Comics.Downloader.Jwt;

public interface ITokenAble
{
    public string? ToToken(string secret);
}