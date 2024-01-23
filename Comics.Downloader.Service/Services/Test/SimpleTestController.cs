using Comics.Downloader.Jwt;
using Comics.Downloader.Model;
using Comics.Downloader.Service.Database;
using Comics.Downloader.Service.Utiliyes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Comics.Downloader.Service.Services.Test
{


    [ApiController]
    [Route("[controller]")]
    public class SimpleTestController
    {
        private readonly IOptions<Appsetting> config;
        private readonly MongoDbContext mongoDbContext;
        private readonly IHttpContextAccessor accessor;

        public SimpleTestController(IOptions<Appsetting> config, MongoDbContext mongoDbContext, IHttpContextAccessor accessor)
        {
            this.config = config;
            this.mongoDbContext = mongoDbContext;
            this.accessor = accessor;
        }

        [HttpGet("database")]
        public TestUser Get()
        {
            var db = this.mongoDbContext.GetDb();
            var userCollection = db.GetCollection<TestUser>(nameof(TestUser));
            var user = userCollection.Find(x => x.Id == new ObjectId("65947953590c1f03e6c4c097")).SingleOrDefault();

            var conf = accessor.HttpContext.User.Claims.ToList();


            if (user is null)
            {
                userCollection.InsertOne(new TestUser { Name = "Horst", Age = 12});
            }

            user = userCollection.Find(x => x.Name.Equals("Horst")).SingleOrDefault();

            return user;
        }

        [HttpGet("generate-token")]
        public dynamic GetGenerateToken(string token, string key = "")
        {
            if (key.IsNullOrEmpty())
            {
                key = config.Value.Jwt.Secret;
            }

            return new
            {
                token = JwtTokenUtility.GenerateToken(key, new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("name", "horst"), new KeyValuePair<string, string>("age", "12") }),
                valid = JwtTokenUtility.ValidateToken(JwtTokenUtility.GenerateToken(key, new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("name", "horst"), new KeyValuePair<string, string>("age", "12") }), key),
                valid2 = JwtTokenUtility.ValidateToken(token, key)
            };
        }

        [HttpGet("auth-test"), Authorize]
        public dynamic GetTokenInfo()
        {
            var user = accessor.HttpContext.User.Claims.ToList().Select(x => new KeyValuePair<string, string>(x.Type, x.Value));
            
            return user;
        }
    }

    public class TestUser
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string ReadId { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public TestUser()
        {
            Id = ObjectId.GenerateNewId();
            ReadId = Id.ToString();
        }
    }
}
