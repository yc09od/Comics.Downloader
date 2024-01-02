using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime.SharedInterfaces;
using Comics.Downloader.Model;
using Comics.Downloader.Service.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace Comics.Downloader.Service.Services.Test
{


    [ApiController]
    [Route("[controller]")]
    public class SimpleTestController
    {
        private readonly IOptions<Appsetting> config;

        public SimpleTestController(IOptions<Appsetting> config)
        {
            this.config = config;
        }

        [HttpGet("database")]
        public TestUser Get()
        {
            var db = new MongoDbContext().GetDb();
            var userCollection = db.GetCollection<TestUser>("TestUser");
            var user = userCollection.Find(x => x.Id == new ObjectId("65947953590c1f03e6c4c097")).SingleOrDefault();

            var conf = config;

            if (user is null)
            {
                userCollection.InsertOne(new TestUser { Name = "Horst", Age = 12});
            }

            user = userCollection.Find(x => x.Name.Equals("Horst")).SingleOrDefault();

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
