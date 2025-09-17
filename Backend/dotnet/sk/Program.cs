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
        Description = "A modern .NET 9 implementation of Semantic Kernel agents with Azure AI integration - Workshop Edition"
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
        // Log configuration status for debugging
        Console.WriteLine("?? Azure OpenAI configuration missing or incomplete");
        Console.WriteLine("Please set either:");
        Console.WriteLine("1. Environment variables (recommended for workshop):");
        Console.WriteLine("   AZURE_OPENAI_ENDPOINT=https://your-resource-name.openai.azure.com");
        Console.WriteLine("   AZURE_OPENAI_API_KEY=your-api-key");
        Console.WriteLine("   AZURE_OPENAI_DEPLOYMENT_NAME=your-deployment");
        Console.WriteLine("2. Or update appsettings.json with Azure OpenAI configuration");
        
        throw new InvalidOperationException("Azure OpenAI configuration required. See console output for setup instructions.");
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

// Add logging with better configuration
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

// Health check endpoint with configuration status
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
        framework = ".NET 9",
        configuration = new
        {
            azureOpenAI = hasAzureOpenAI ? "configured" : "missing",
            azureAIFoundry = hasAzureFoundry ? "configured" : "missing",
            frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3001",
            configurationSource = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") != null ? "environment" : "appsettings"
        }
    };
});

// Configuration info endpoint for workshop
app.MapGet("/api/config", (Microsoft.Extensions.Options.IOptions<AzureAIConfig> configOptions) =>
{
    var azureConfig = configOptions.Value;
    var endpoint = azureConfig?.AzureOpenAI?.Endpoint;
    var projectEndpoint = azureConfig?.AzureAIFoundry?.ProjectEndpoint;
    
    return new
    {
        configurationSource = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") != null ? "environment_variables" : "appsettings_json",
        azureOpenAI = new
        {
            configured = !string.IsNullOrEmpty(endpoint) && 
                        !string.IsNullOrEmpty(azureConfig?.AzureOpenAI?.ApiKey),
            endpoint = endpoint != null ? endpoint.Substring(0, Math.Min(30, endpoint.Length)) + "..." : null,
            deployment = azureConfig?.AzureOpenAI?.DeploymentName,
            apiVersion = azureConfig?.AzureOpenAI?.ApiVersion
        },
        azureAIFoundry = new
        {
            configured = !string.IsNullOrEmpty(projectEndpoint),
            projectEndpoint = projectEndpoint != null ? projectEndpoint.Substring(0, Math.Min(30, projectEndpoint.Length)) + "..." : null,
            hasPeopleAgent = !string.IsNullOrEmpty(azureConfig?.AzureAIFoundry?.PeopleAgentId),
            hasKnowledgeAgent = !string.IsNullOrEmpty(azureConfig?.AzureAIFoundry?.KnowledgeAgentId)
        },
        frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3001"
    };
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "8002";
app.Urls.Add($"http://localhost:{port}");

Console.WriteLine($"?? .NET Semantic Kernel Agents API starting on port {port}");
Console.WriteLine($"?? Swagger UI available at: http://localhost:{port}");
Console.WriteLine($"?? Configuration endpoint: http://localhost:{port}/api/config");
Console.WriteLine($"?? Configuration source: {(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") != null ? "Environment variables" : "appsettings.json")}");

app.Run();