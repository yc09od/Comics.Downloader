using System.Net;
using Comics.Downloader.Jwt;
using Comics.Downloader.Model;
using Comics.Downloader.Model.DataObject;
using Comics.Downloader.Model.ViewModel.User;
using Comics.Downloader.Service.Database;
using Comics.Downloader.Service.Model.AuthModel;
using Comics.Downloader.Service.Response.UserResponse;
using Comics.Downloader.Service.Utiliyes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Comics.Downloader.Service.Services
{
    [ApiController, Route("/auth")]
    public class AuthorizationController
    {
        private readonly IOptions<Appsetting> _appsetting;
        private readonly MongoDbContext mongoDbContext;
        private readonly IHttpContextAccessor accessor;

        public AuthorizationController(IHttpContextAccessor accessor, MongoDbContext mongoDbContext,
            IOptions<Appsetting> config)
        {
            this.accessor = accessor;
            this.mongoDbContext = mongoDbContext;
            this._appsetting = config;
        }

        [HttpPost("basic/login")]
        public PostAuthValidationResponse PostAuthValidation([FromBody] PostAuthValidationRequest request)
        {
            var db = this.mongoDbContext.GetDb();
            var users = db.GetCollection<User>(nameof(User));

            var existingUsers = users.Find(x =>
                    string.Equals(x.Username, request.Username, StringComparison.CurrentCultureIgnoreCase) ||
                    string.Equals(x.Email, request.Username, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            var validUser =
                existingUsers.SingleOrDefault(x =>
                    string.Equals(x.HashedPassword, request.Password.ToHashedPassword()));

            if (validUser != null)
            {
                return new PostAuthValidationResponse
                {
                    Result = validUser.ToViewModel(_appsetting.Value.Jwt.Secret)
                };
            }

            var error = new HttpRequestException("User is not existing or password is not correct.", null,
                HttpStatusCode.Forbidden);

            throw error;
        }
    }
}