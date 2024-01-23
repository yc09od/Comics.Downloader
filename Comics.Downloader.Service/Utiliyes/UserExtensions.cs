using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comics.Downloader.Jwt;
using Comics.Downloader.Model.DataObject;
using Comics.Downloader.Model.ViewModel.User;
using Newtonsoft.Json.Linq;

namespace Comics.Downloader.Service.Utiliyes
{
    public static class UserExtensions
    {
        public static UserViewModel ToViewModel(this User user, string s)
        {
            var jwtToken = user.ToToken(s);
            var expireDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(long.Parse(jwtToken.ToDictionary()["exp"]));
            return new UserViewModel
            {
                Email = user.Email,
                Id = user.Id,
                Token = user.ToToken(s),
                UserName = user.Username,
                ExpireTime = expireDateTimeOffset
            };
        }
    }
}
