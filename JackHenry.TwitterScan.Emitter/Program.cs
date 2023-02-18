using JackHenry.TwitterScan;
using Microsoft.AspNetCore.Http.Json;
using RossWright;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
});

var authCfg = builder.Services.AddJwtAuth(builder.Configuration);

var app = builder.Build();
app.UseHttpsRedirection();

var streamer = new Streamer();
app.MapGet("/stream", (int? rate) => streamer.Stream(rate));

app.MapGet("/issueJwt", () => authCfg.MakeAccessToken());

app.Run();