namespace Comics.Downloader.Jwt
{
    public interface IUser: ITokenAble
    {
        public string Username { get; set; }

        public string Id { get; set; }

        public string Email { get; set; }
    }
}
