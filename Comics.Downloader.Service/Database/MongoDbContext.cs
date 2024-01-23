using Comics.Downloader.Service.Utiliyes;
using MongoDB.Driver;

namespace Comics.Downloader.Service.Database
{
    public class MongoDbContext
    {
        private string _database { get; set; } = "mongoDbTest";
        private string _connectionString = "mongodb://localhost:27017/";
        public MongoClient _client;

        public MongoDbContext(string connectionString = "", string database = "")
        {
            if (!connectionString.IsNullOrEmpty())
            {
                _connectionString = "mongodb://localhost:27017/";
            }

            if (!database.IsNullOrEmpty())
            {
                _database = database;
            }

            _client = new MongoClient(_connectionString);
        }

        public IMongoDatabase GetDb(string database = "")
        {
            if (database.IsNullOrEmpty())
            {
                database = _database;
            }

            return _client.GetDatabase(_database);
        }
    }
}
