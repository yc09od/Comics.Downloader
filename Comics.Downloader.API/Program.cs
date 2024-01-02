using Comics.Downloader.Model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var configBuilder = new ConfigurationBuilder().SetBasePath(builder.Environment.ContentRootPath).AddJsonFile("appsettings.json", optional:true, reloadOnChange:false);
var config = configBuilder.Build();

builder.Services.Configure<Appsetting>(config.GetSection(nameof(Appsetting)));

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
