using System.Text;
using Comics.Downloader.Jwt;
using Comics.Downloader.Model;
using Comics.Downloader.Service.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.OperationFilter<JwtSwaggerOperationFilter>();
});

var configBuilder = new ConfigurationBuilder().SetBasePath(builder.Environment.ContentRootPath).AddJsonFile("appsettings.json", optional:true, reloadOnChange:false);
var config = configBuilder.Build();

builder.Services.Configure<Appsetting>(config.GetSection(nameof(Appsetting)));


var appsetting = builder.Configuration.GetSection(nameof(Appsetting)).Get<Appsetting>();

builder.Services.AddSingleton<MongoDbContext>(x => new MongoDbContext(appsetting?.Database?.MongoDb?.ConnectingString));

// auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //.AddScheme<JwtBearerOptions, CustomJwtHandler>(JwtBearerDefaults.AuthenticationScheme, options => { });
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = JwtTokenUtility.GetTokenValidationParameters(appsetting.Jwt.Secret);
    });

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
