namespace Comics.Downloader.Model.ViewModel.User;

public class UserViewModel
{
    public string Id { get; set; }
    public string UserName { get; set; }

    public string? Token { get; set; }

    public string Email { get; set; }

    public DateTimeOffset ExpireTime { get; set; }
}