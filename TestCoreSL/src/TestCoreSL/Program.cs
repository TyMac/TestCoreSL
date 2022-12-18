using TestCoreSL;
using Amazon.SecretsManager;
using Amazon;
using Amazon.SecretsManager.Model;
using Newtonsoft.Json;
// using Microsoft.EntityFrameworkCore;
// using System.Reflection.Metadata.Ecma335;
// using Microsoft.AspNetCore.Mvc;
// using MediatR;
// using System.Reflection.Metadata;
// using Amazon.Runtime;
// using System.Runtime.InteropServices;
// using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);
var secret_endpoint = builder.Configuration["AWS_SECRETS"] ?? builder.Configuration["secretsmanager:endpoint"];
AmazonSecretsManagerConfig config = new() { ServiceURL = secret_endpoint };
builder.Services.AddScoped<IAmazonSecretsManager>(a =>
    // new AmazonSecretsManagerClient(RegionEndpoint.USEast1)
    new AmazonSecretsManagerClient(config)
);


IAmazonSecretsManager secretsManager = new AmazonSecretsManagerClient(RegionEndpoint.USEast1);

// var version = await secretsManager.ListSecretVersionIdsAsync(ListVersionIdsRequest);

var request = new GetSecretValueRequest
{
    SecretId = "prod_coremetrcs__passwd",
    // VersionId = version.Versions.First().VersionId
};

request.VersionStage = "AWSCURRENT";

var prod_coremetrcs__passwd = await secretsManager.GetSecretValueAsync(request);
string? secret = "";

GetSecretValueResponse? response = null;
response = secretsManager.GetSecretValueAsync(request).Result;

if (response.SecretString != null)
{
    secret = response.SecretString;
}

Dictionary<string, string> jsonsecret = JsonConvert.DeserializeObject<Dictionary<string, string>>(secret);

var ListVersionIdsRequest = new ListSecretVersionIdsRequest
{
    SecretId = "prod_coremetrcs__passwd"
};

var smpasswd = jsonsecret["passwd"];

var server = builder.Configuration["RDS_HOSTNAME"] ?? builder.Configuration["sqlserver:hostname"];
var port = builder.Configuration["RDS_PORT"] ?? builder.Configuration["sqlserver:port"];
var user = builder.Configuration["RDS_USERNAME"] ?? builder.Configuration["sqlserver:user"];
var password = builder.Configuration["RDS_PASSWORD"] ?? builder.Configuration["sqlserver:passwd"];
// var password = builder.Configuration["smpasswd"] ?? builder.Configuration["sqlserver:passwd"];
var database = builder.Configuration["RDS_DB_NAME"] ?? builder.Configuration["sqlserver:database"];

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer($"Server={server},{port};Initial Catalog={database};User ID={user};Password={password}",
    providerOptions => providerOptions.EnableRetryOnFailure()));

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        builder =>
        {
            builder.WithOrigins("http://localhost");
        });
});

// Add services to the container.
// builder.Services.AddControllers();

// Add AWS Lambda support. When application is run in Lambda Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer. This
// package will act as the webserver translating request and responses between the Lambda event source and ASP.NET Core.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
    dataContext.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseCors(builder => builder
 .AllowAnyOrigin());

// app.UseAuthorization();
// app.MapControllers();

async Task<List<CoreMetric>> GetAllCoreMetrics(DataContext context) =>
    await context.CoreMetrics.ToListAsync();

app.MapGet("/", () => smpasswd);

app.MapGet("/coremetric", async (DataContext context) => 
    await context.CoreMetrics.ToListAsync());

app.MapGet("/coremetric/{id}", async (DataContext context, int id) => 
    await context.CoreMetrics.FindAsync(id) is CoreMetric metric ? 
    Results.Ok(metric) :
    Results.NotFound("Metric not found"));

/*
app.MapGet("/search", (CoreMetric criteria, DataContext context) =>
 {
    IQueryable<CoreMetric> vquery = context.CoreMetrics.Where(m => m.MetricHost == criteria.MetricHost);
     
     // IQueryable<CoreMetric> vquery = context.CoreMetrics;
     if (criteria.MetricHost is not null)
     {
         var query = vquery.Where(m => m.MetricHost == criteria.MetricHost);
         // query = query.Where(m => m.MetricHost.Equals(criteria.MetricHost));
     }
     
     return vquery.ToListAsync();
     /*
     return Results.Ok(context.CoreMetrics.Select(x => new
     {
         x.MetricHost,
         x.MetricValue
     }).Where(y => y.MetricHost == context)
     );
     */


app.MapGet("/computer/{hostname}", (DataContext context, string hostname) =>
{
    return Results.Ok(context.CoreMetrics.Select(x => new
    {
        x.MetricHost,
        x.MetricValue
    }).Where(y => y.MetricHost == hostname)
    ); 
});

app.MapGet("/hostnames", (DataContext context) =>
{
    return Results.Ok(context.CoreMetrics.Select(x => new
    {
        x.MetricHost
    }));
});

app.MapGet("/search", (DataContext context, string? host) =>
{
    return Results.Ok(context.CoreMetrics.Select(x => new
    {
        x.MetricHost,
        x.MetricValue
    }).Where(y => y.MetricHost == host)
    );
 });

app.Run();
