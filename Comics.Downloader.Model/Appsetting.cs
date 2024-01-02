namespace Comics.Downloader.Model
{
    public class Appsetting
    {
        public Database Database { get; set; }
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
