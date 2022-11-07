using TestCoreSL;
using Amazon.SecretsManager;
using Amazon;
using Amazon.SecretsManager.Model;
using Newtonsoft.Json;

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

var passwd = jsonsecret["passwd"];

var server = builder.Configuration["RDS_HOSTNAME"] ?? builder.Configuration["sqlserver:hostname"];
var port = builder.Configuration["RDS_PORT"] ?? builder.Configuration["sqlserver:port"];
var user = builder.Configuration["RDS_USERNAME"] ?? builder.Configuration["sqlserver:user"];
var password = builder.Configuration["RDS_PASSWORD"] ?? builder.Configuration["sqlserver:passwd"];
// var password = builder.Configuration["rdspasswd"] ?? builder.Configuration["sqlserver:passwd"];
// var password = builder.Configuration["passwd"] ?? builder.Configuration["sqlserver:passwd"];
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

app.MapGet("/", () => jsonsecret["passwd"]);

app.MapGet("/coremetric", async (DataContext context) => await context.CoreMetrics.ToListAsync());

app.Run();
