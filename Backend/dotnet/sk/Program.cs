using DotNetSemanticKernel.Configuration;
using DotNetSemanticKernel.Services;
using Microsoft.SemanticKernel;
using Microsoft.OpenApi.Models;

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

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? 
                         builder.Configuration["FRONTEND_URL"] ?? 
                         "http://localhost:3001";
        policy.WithOrigins(frontendUrl)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
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
            ApiVersion = azureOpenAIApiVersion ?? "2024-02-01"
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
            apiVersion: config.AzureOpenAI.ApiVersion ?? "2024-02-01");
            
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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", ".NET Semantic Kernel Agents API V1");
        c.RoutePrefix = string.Empty; // Make Swagger the default page
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

// Essential endpoints only - matching Python FastAPI structure

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
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "8000";
app.Urls.Add($"http://localhost:{port}");

Console.WriteLine($"?? .NET Semantic Kernel Agents API");
Console.WriteLine($"?? Swagger UI: http://localhost:{port}");
Console.WriteLine($"? Endpoints: /agents, /chat, /group-chat, /health");

app.Run();