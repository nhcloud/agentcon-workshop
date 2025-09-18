using DotNetSemanticKernel.Configuration;
using DotNetSemanticKernel.Services;
using Microsoft.SemanticKernel;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file if it exists
if (File.Exists(".env"))
{
    foreach (var line in File.ReadAllLines(".env"))
    {
        if (line.StartsWith("#") || !line.Contains("=")) continue;
        
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
        }
    }
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = ".NET Semantic Kernel Agents API", 
        Version = "v1",
        Description = "A modern .NET 9 implementation of Semantic Kernel agents with Azure AI integration"
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
    
    // Azure AI Foundry configuration
    if (!string.IsNullOrEmpty(projectEndpoint))
    {
        options.AzureAIFoundry = new AzureAIFoundryConfig
        {
            ProjectEndpoint = projectEndpoint,
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

// Add Semantic Kernel services
builder.Services.AddScoped<IKernelBuilder>(provider =>
{
    var config = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AzureAIConfig>>().Value;
    var kernelBuilder = Kernel.CreateBuilder();
    
    // Try Azure OpenAI configuration (matching Python template structure)
    if (!string.IsNullOrEmpty(config?.AzureOpenAI?.Endpoint) && !string.IsNullOrEmpty(config.AzureOpenAI.ApiKey))
    {
        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: config.AzureOpenAI.DeploymentName ?? "gpt-4o",
            endpoint: config.AzureOpenAI.Endpoint,
            apiKey: config.AzureOpenAI.ApiKey,
            apiVersion: config.AzureOpenAI.ApiVersion ?? "2024-10-21");
            
        Console.WriteLine($"? Configured Azure OpenAI: {config.AzureOpenAI.Endpoint}");
        Console.WriteLine($"?? Using deployment: {config.AzureOpenAI.DeploymentName}");
    }
    else
    {
        throw new InvalidOperationException("Azure OpenAI configuration required. Set AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_API_KEY environment variables.");
    }
    
    return kernelBuilder;
});

builder.Services.AddScoped<Kernel>(provider =>
{
    var kernelBuilder = provider.GetRequiredService<IKernelBuilder>();
    return kernelBuilder.Build();
});

// Add agent services
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IGroupChatService, GroupChatService>();
builder.Services.AddSingleton<ISessionManager, SessionManager>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", ".NET Semantic Kernel Agents API V1");
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
        agents = new { status = "available" },
        session_manager = "operational"
    };
}).WithName("GetHealth").WithTags("Health");

// Reset endpoint that frontend expects
app.MapPost("/reset", async (HttpContext context,
    DotNetSemanticKernel.Services.ISessionManager sessionManager,
    ILogger<Program> logger) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    
    try
    {
        var requestData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(body, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        string? sessionId = null;
        if (requestData?.ContainsKey("session_id") == true)
        {
            sessionId = requestData["session_id"]?.ToString();
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            await sessionManager.ClearSessionAsync(sessionId);
        }

        return Results.Ok(new { 
            status = "success", 
            message = "Session reset successfully",
            session_id = sessionId
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error resetting session");
        return Results.StatusCode(500);
    }
}).WithName("ResetSession").WithTags("Session");

var port = Environment.GetEnvironmentVariable("PORT") ?? "8000";

// Configure URLs - only HTTP for development to avoid certificate issues
app.Urls.Add($"http://localhost:{port}");

Console.WriteLine($"?? .NET Semantic Kernel Agents API");
Console.WriteLine($"?? Swagger UI: http://localhost:{port}");
Console.WriteLine($"? Endpoints: /agents, /chat, /group-chat, /health");
Console.WriteLine($"?? Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"?? CORS: {(app.Environment.IsDevelopment() ? "Development (Allow All)" : "Production (Restricted)")}");

app.Run();