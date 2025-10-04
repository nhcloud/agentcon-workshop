using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using DotNetEnv;
using DotNetAgentFramework.Configuration;
using DotNetAgentFramework.Services;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
Env.Load();

// Add YAML configuration support
builder.Configuration.AddYamlFile("config.yml", optional: true, reloadOnChange: true);

// Configure request timeout - increase from default 20 seconds to 2 minutes
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(2);
});

// Configure HttpClient with longer timeout for external API calls
builder.Services.AddHttpClient("AzureOpenAI", client =>
{
    client.Timeout = TimeSpan.FromMinutes(3); // 3 minutes for AI operations
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = ".NET Agent Framework API", 
        Version = "v1",
        Description = "A modern .NET 9 implementation of Microsoft Agent Framework with Azure AI integration"
    });
});

// Configure CORS - more permissive for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? 
                         builder.Configuration["FRONTEND_URL"] ?? 
                         "http://localhost:3001";
        
        if (builder.Environment.IsDevelopment())
        {
            // More permissive CORS for development
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(frontendUrl)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Add configuration - supporting both appsettings.json and environment variables
builder.Services.Configure<AzureAIConfig>(options =>
{
    // Try environment variables first (like Python version)
    var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
    var azureOpenAIDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");
    var azureOpenAIApiVersion = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION");
    
    var projectEndpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT");
    var peopleAgentId = Environment.GetEnvironmentVariable("PEOPLE_AGENT_ID");
    var knowledgeAgentId = Environment.GetEnvironmentVariable("KNOWLEDGE_AGENT_ID");
    
    if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIApiKey))
    {
        // Use environment variables (matching Python setup)
        options.AzureOpenAI = new AzureOpenAIConfig
        {
            Endpoint = azureOpenAIEndpoint,
            ApiKey = azureOpenAIApiKey,
            DeploymentName = azureOpenAIDeployment ?? "gpt-4o",
            ApiVersion = azureOpenAIApiVersion ?? "2024-10-21"
        };
    }
    else
    {
        // Fallback to appsettings.json
        var config = builder.Configuration.GetSection("AzureAI").Get<AzureAIConfig>();
        if (config?.AzureOpenAI != null)
        {
            options.AzureOpenAI = config.AzureOpenAI;
        }
    }
    
    // Azure AI Foundry configuration using new Agent Framework
    if (!string.IsNullOrEmpty(projectEndpoint))
    {
        options.AzureAIFoundry = new AzureAIFoundryConfig
        {
            ProjectEndpoint = projectEndpoint,
            ManagedIdentityClientId = Environment.GetEnvironmentVariable("MANAGED_IDENTITY_CLIENT_ID"), // Add Managed Identity Client ID support
            PeopleAgentId = peopleAgentId,
            KnowledgeAgentId = knowledgeAgentId
        };
    }
    else
    {
        var config = builder.Configuration.GetSection("AzureAI").Get<AzureAIConfig>();
        if (config?.AzureAIFoundry != null)
        {
            options.AzureAIFoundry = config.AzureAIFoundry;
        }
    }
});

// Add agent configuration from YAML
builder.Services.Configure<AppConfig>(builder.Configuration);

// Register agent instructions service
builder.Services.AddSingleton<AgentInstructionsService>();

// Add Agent Framework services - simplified for the new Agent Framework pattern
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IGroupChatService, GroupChatService>();
builder.Services.AddSingleton<ISessionManager, SessionManager>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Add request timeout middleware
app.Use(async (context, next) =>
{
    // Set longer timeout for group chat endpoints
    if (context.Request.Path.StartsWithSegments("/group-chat"))
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // 5 minutes for group chat
        context.RequestAborted = cts.Token;
    }
    else if (context.Request.Path.StartsWithSegments("/chat"))
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2)); // 2 minutes for single chat
        context.RequestAborted = cts.Token;
    }
    
    await next();
});

// Add request logging middleware for debugging
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/group-chat") && context.Request.Method == "POST")
    {
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;
        
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Group-chat request body: {RequestBody}", body);
    }
    
    await next();
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", ".NET Agent Framework API V1");
        c.RoutePrefix = string.Empty; // Make Swagger the default page
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
        c.EnableValidator();
        c.SupportedSubmitMethods(SubmitMethod.Get, SubmitMethod.Post, SubmitMethod.Put, SubmitMethod.Delete, SubmitMethod.Patch);
    });
}

// Important: Use CORS before other middleware
app.UseCors("AllowFrontend");

// Only use HTTPS redirection in production or when explicitly configured
if (!app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("ForceHttps"))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", (Microsoft.Extensions.Options.IOptions<AzureAIConfig> configOptions) => 
{
    var azureConfig = configOptions.Value;
    var hasAzureOpenAI = !string.IsNullOrEmpty(azureConfig?.AzureOpenAI?.Endpoint) && 
                        !string.IsNullOrEmpty(azureConfig.AzureOpenAI.ApiKey);
    var hasAzureFoundry = !string.IsNullOrEmpty(azureConfig?.AzureAIFoundry?.ProjectEndpoint);
    
    return new 
    { 
        status = "healthy", 
        timestamp = DateTime.UtcNow,
        framework = "Microsoft Agent Framework",
        configuration = new
        {
            azure_openai = hasAzureOpenAI ? "configured" : "missing",
            azure_ai_foundry = hasAzureFoundry ? "configured" : "missing",
            foundry_agents = new
            {
                people_agent = !string.IsNullOrEmpty(azureConfig?.AzureAIFoundry?.PeopleAgentId) ? "configured" : "missing",
                knowledge_agent = !string.IsNullOrEmpty(azureConfig?.AzureAIFoundry?.KnowledgeAgentId) ? "configured" : "missing"
            },
            timeout_settings = new
            {
                request_timeout = "5 minutes (group chat), 2 minutes (single chat)",
                http_client_timeout = "3 minutes",
                keep_alive_timeout = "5 minutes"
            }
        },
        agents = new { status = "available" },
        session_manager = "operational"
    };
}).WithName("GetHealth").WithTags("Health");

// Start the application and keep it running
Console.WriteLine("?? Starting .NET Agent Framework API...");
Console.WriteLine("?? Swagger UI available at: http://localhost:8000");
Console.WriteLine("?? Health endpoint: http://localhost:8000/health");
Console.WriteLine("?? Microsoft Agent Framework with Azure AI Foundry integration ready!");

app.Run();