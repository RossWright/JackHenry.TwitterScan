using JackHenry.TwitterScan;
using Microsoft.AspNetCore.Http.Json;
using RossWright;
using System.Text.Json.Serialization;
using FromQuery = Microsoft.AspNetCore.Mvc.FromQueryAttribute;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
});

var authCfg = builder.Services.AddJwtAuth(builder.Configuration);

var app = builder.Build();
app.UseHttpsRedirection();

var streamer = new Streamer();
app.MapGet("/stream", (
    int? rate,
    [FromQuery(Name = "tweet.fields")] string? tweetFieldsStr) 
    => streamer.Stream(rate, tweetFieldsStr));

app.MapGet("/issueJwt", () => authCfg.MakeAccessToken());

app.Run();