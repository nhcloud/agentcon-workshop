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

// Add root-level endpoints for frontend compatibility
// These endpoints delegate to the existing API controllers

// Chat endpoint - delegates to ChatController
app.MapPost("/chat", async (HttpContext context, 
    DotNetSemanticKernel.Services.IAgentService agentService,
    DotNetSemanticKernel.Services.ISessionManager sessionManager,
    ILogger<Program> logger) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var request = System.Text.Json.JsonSerializer.Deserialize<DotNetSemanticKernel.Models.ChatRequest>(body, new System.Text.Json.JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
    });

    if (request == null || string.IsNullOrWhiteSpace(request.Message))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "Message is required" }));
        return;
    }

    try
    {
        var agentName = request.Agent ?? "generic";
        var response = await agentService.ChatWithAgentAsync(agentName, request);
        
        // Store conversation in session
        var sessionId = request.SessionId ?? await sessionManager.CreateSessionAsync();
        response.SessionId = sessionId;

        var userMessage = new DotNetSemanticKernel.Models.GroupChatMessage
        {
            Content = request.Message,
            Agent = "user",
            Timestamp = DateTime.UtcNow,
            Turn = 0
        };
        
        var agentMessage = new DotNetSemanticKernel.Models.GroupChatMessage
        {
            Content = response.Content,
            Agent = response.Agent,
            Timestamp = response.Timestamp,
            Turn = 1
        };

        await sessionManager.AddMessageToSessionAsync(sessionId, userMessage);
        await sessionManager.AddMessageToSessionAsync(sessionId, agentMessage);

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
        }));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in chat endpoint");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "Internal server error" }));
    }
});

// Group chat endpoint - delegates to GroupChatController
app.MapPost("/group-chat", async (HttpContext context,
    DotNetSemanticKernel.Services.IGroupChatService groupChatService,
    DotNetSemanticKernel.Services.IAgentService agentService,
    ILogger<Program> logger) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var request = System.Text.Json.JsonSerializer.Deserialize<DotNetSemanticKernel.Models.GroupChatRequest>(body, new System.Text.Json.JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
    });

    if (request == null || string.IsNullOrWhiteSpace(request.Message))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "Message is required" }));
        return;
    }

    if (request.Agents == null || !request.Agents.Any())
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "At least one agent must be specified" }));
        return;
    }

    try
    {
        // Validate agents exist
        var availableAgents = await agentService.GetAvailableAgentsAsync();
        var availableAgentNames = availableAgents.Select(a => a.Name).ToHashSet();
        var invalidAgents = request.Agents.Where(a => !availableAgentNames.Contains(a)).ToList();
        
        if (invalidAgents.Any())
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { 
                error = "Invalid agents specified", 
                invalid_agents = invalidAgents,
                available_agents = availableAgentNames.ToList()
            }));
            return;
        }

        var response = await groupChatService.StartGroupChatAsync(request);
        
        // Transform response to match frontend expectations
        var responseMessages = response.Messages?.Where(m => m.Agent != "user").ToList() ?? new List<DotNetSemanticKernel.Models.GroupChatMessage>();
        var responses = responseMessages.Select(m => new
        {
            agent = m.Agent,
            content = m.Content,
            message_id = m.MessageId,
            metadata = new { }
        }).ToList();
        
        var frontendResponse = new
        {
            conversation_id = response.SessionId,
            total_turns = response.TotalTurns,
            active_participants = response.Messages?.Select(m => m.Agent).Distinct().Where(a => a != "user").ToList() ?? new List<string>(),
            responses = responses,
            summary = response.Summary,
            content = response.Summary ?? response.Messages?.LastOrDefault()?.Content,
            metadata = new { }
        };
        
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(frontendResponse, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
        }));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in group chat endpoint");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "Internal server error" }));
    }
});

// Agents endpoint - delegates to AgentsController
app.MapGet("/agents", async (DotNetSemanticKernel.Services.IAgentService agentService, ILogger<Program> logger) =>
{
    try
    {
        var agents = await agentService.GetAvailableAgentsAsync();
        var agentList = agents.ToList();
        
        return Results.Ok(new { 
            agent_count = agentList.Count,
            agents = agentList
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving agents");
        return Results.Problem("Internal server error while retrieving agents", statusCode: 500);
    }
});

// Reset endpoint
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

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { 
            status = "success", 
            message = "Session reset successfully",
            session_id = sessionId
        }));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error resetting session");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "Internal server error" }));
    }
});

// Group chat templates endpoint
app.MapGet("/group-chat/templates", (ILogger<Program> logger) =>
{
    try
    {
        // Return empty templates array for now - can be extended later
        return Results.Ok(new { 
            templates = new List<object>()
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving group chat templates");
        return Results.Problem("Internal server error while retrieving templates", statusCode: 500);
    }
});

// Active group chats endpoint
app.MapGet("/group-chats", async (DotNetSemanticKernel.Services.ISessionManager sessionManager, ILogger<Program> logger) =>
{
    try
    {
        var activeSessions = await sessionManager.GetActiveSessionsAsync();
        var groupChats = new List<object>();
        
        foreach (var sessionId in activeSessions)
        {
            try
            {
                var sessionInfo = await sessionManager.GetSessionInfoAsync(sessionId);
                groupChats.Add(new
                {
                    session_id = sessionId,
                    created_at = sessionInfo.CreatedAt,
                    last_activity = sessionInfo.LastActivity,
                    message_count = sessionInfo.MessageCount,
                    agent_types = sessionInfo.AgentTypes
                });
            }
            catch
            {
                // Skip invalid sessions
            }
        }
        
        return Results.Ok(new { 
            group_chats = groupChats
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving active group chats");
        return Results.Problem("Internal server error while retrieving group chats", statusCode: 500);
    }
});

// Create group chat from template endpoint
app.MapPost("/group-chat/from-template", async (HttpContext context, ILogger<Program> logger) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    
    try
    {
        var requestData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(body, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        string? templateName = null;
        if (requestData?.ContainsKey("template_name") == true)
        {
            templateName = requestData["template_name"]?.ToString();
        }

        // For now, return a basic response - can be extended later with actual template logic
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { 
            error = "Templates not implemented yet",
            template_name = templateName
        }));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating group chat from template");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "Internal server error" }));
    }
});

// Reset group chat session endpoint
app.MapPost("/group-chat/{sessionId}/reset", async (string sessionId,
    DotNetSemanticKernel.Services.ISessionManager sessionManager,
    ILogger<Program> logger) =>
{
    try
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return Results.BadRequest(new { error = "Session ID is required" });
        }

        await sessionManager.ClearSessionAsync(sessionId);
        
        return Results.Ok(new { 
            status = "success", 
            message = "Group chat session reset successfully",
            session_id = sessionId
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error resetting group chat session {SessionId}", sessionId);
        return Results.Problem("Internal server error while resetting group chat", statusCode: 500);
    }
});

// Delete group chat session endpoint
app.MapDelete("/group-chat/{sessionId}", async (string sessionId,
    DotNetSemanticKernel.Services.ISessionManager sessionManager,
    ILogger<Program> logger) =>
{
    try
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return Results.BadRequest(new { error = "Session ID is required" });
        }

        await sessionManager.ClearSessionAsync(sessionId);
        
        return Results.Ok(new { 
            status = "success", 
            message = "Group chat session deleted successfully",
            session_id = sessionId
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error deleting group chat session {SessionId}", sessionId);
        return Results.Problem("Internal server error while deleting group chat", statusCode: 500);
    }
});

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

var port = Environment.GetEnvironmentVariable("PORT") ?? "8000";
app.Urls.Add($"http://localhost:{port}");

Console.WriteLine($"?? .NET Semantic Kernel Agents API starting on port {port}");
Console.WriteLine($"?? Swagger UI available at: http://localhost:{port}");
Console.WriteLine($"?? Configuration endpoint: http://localhost:{port}/api/config");
Console.WriteLine($"?? Configuration source: {(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") != null ? "Environment variables" : "appsettings.json")}");

app.Run();