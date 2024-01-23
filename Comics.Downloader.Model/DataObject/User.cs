using Comics.Downloader.Jwt;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Comics.Downloader.Model.DataObject;

public class User : IUser
{
    [BsonId] public ObjectId BsonId { get; set; }

    public string Id { get; set; }

    public string Username { get; set; }

    public string HashedPassword { get; set; }

    public string Email { get; set; }

    public string? ToToken(string secret)
    {
        return JwtTokenUtility.GenerateToken(secret,
            new List<KeyValuePair<string, string>>
                { new(nameof(this.Id), this.Id), new(nameof(Username), Username), new(nameof(Email), Email) });
    }

    public User(string username, string hashedPassword) :
        this(ObjectId.GenerateNewId(), username, hashedPassword, string.Empty)
    {
    }

    public User(string username, string hashedPassword, string email) : this(ObjectId.GenerateNewId(), username,
        hashedPassword,
        email)
    {
    }

    public User(ObjectId bsonId, string username, string hashedPassword, string email)
    {
        BsonId = bsonId;
        Id = bsonId.ToString();
        Username = username;
        HashedPassword = hashedPassword;
        Email = email;
    }
}