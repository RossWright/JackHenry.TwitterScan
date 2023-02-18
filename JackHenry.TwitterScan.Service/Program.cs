global using RossWright;
using JackHenry.TwitterScan.Service;

var builder = WebApplication.CreateBuilder(args);

// Set up Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
var assemblies = AppDomain.CurrentDomain.LoadLocalAssemblies(new string[] { "JackHenry" });
builder.Services.AutoloadConfigObjects(builder.Configuration, assemblies);
builder.Services.AutoloadServices(assemblies,
    allowMultiple: new Type[] { typeof(ITweetProcessor)});

builder.Services.AddHttpClient();

builder.Services.AddHostedService<TwitterStreamReaderBackgroundService>();

// Add API to the container.
var CORS_POLICY_NAME = "clientCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CORS_POLICY_NAME,
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});
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

app.UseCors(CORS_POLICY_NAME);

app.MapControllers();

app.Run();
