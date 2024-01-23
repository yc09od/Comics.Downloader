using System.Net;
using Comics.Downloader.Jwt;
using Comics.Downloader.Model;
using Comics.Downloader.Model.DataObject;
using Comics.Downloader.Service.Database;
using Comics.Downloader.Service.Model.UserModel;
using Comics.Downloader.Service.Response.UserResponse;
using Comics.Downloader.Service.Utiliyes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Comics.Downloader.Service.Services
{
    [ApiController]
    [Route("/user")]
    public class UserController
    {
        private readonly IOptions<Appsetting> _appsetting;
        private readonly MongoDbContext _mongoDbContext;
        private readonly IHttpContextAccessor _accessor;
        public UserController(IOptions<Appsetting> appsetting, MongoDbContext mongoDbContext, IHttpContextAccessor accessor)
        {
            _appsetting = appsetting;
            _mongoDbContext = mongoDbContext;
            _accessor = accessor;
        }

        [HttpPost]
        [Route("basic/register")]
        public PostUserRegisterResponse PostUserRegister([FromBody] PostUserRegisterRequest request)
        {
            var db = _mongoDbContext.GetDb();
            var userContext = db.GetCollection<Downloader.Model.DataObject.User>(nameof(Downloader.Model.DataObject.User));

            if (userContext.Find(x =>
                    x.Email.Equals(request.Username, StringComparison.CurrentCultureIgnoreCase) ||
                    x.Username.Equals(request.Username, StringComparison.CurrentCultureIgnoreCase)).Any())
            {
                throw new HttpRequestException("Username is existing", null, HttpStatusCode.BadRequest);
            }

            var user = new User(request.Username, request.Password.ToHashedPassword());
            userContext.InsertOne(user);
            
            var result  = user.ToViewModel(_appsetting.Value.Jwt.Secret);

            return new PostUserRegisterResponse { Result = result };
        }
    }
}
