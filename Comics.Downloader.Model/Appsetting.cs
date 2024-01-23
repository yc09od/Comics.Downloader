namespace Comics.Downloader.Model
{
    public class Appsetting
    {
        public Jwt Jwt { get; set; }
        public Database Database { get; set; }
    }

    public class Jwt
    {
        public int ExpireInHours { get; set; }

        public string Secret { get; set; }
    }

    public class Database
    {
        public MongoDb MongoDb { get; set; }
    }

    public class MongoDb
    {
        public string ConnectingString { get; set; }
    }
}
