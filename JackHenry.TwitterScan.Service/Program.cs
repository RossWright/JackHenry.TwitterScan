using JackHenry.TwitterScan.Service;

var builder = WebApplication.CreateBuilder(args);

// Set up Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Load and Add the Stream Counter Config to the container
var twitterReceiverConfiguration = new TwitterReceiverConfiguration();
builder.Configuration.Bind("TwitterConnection", twitterReceiverConfiguration);
builder.Services.AddSingleton(twitterReceiverConfiguration);

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ITweetStatRepository, TweetStatRepository>();
builder.Services.AddScoped<ITweetReceiver, TweetReciever>();
builder.Services.AddHostedService<TweetReceiverBackgroundService>();

// Add API to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
