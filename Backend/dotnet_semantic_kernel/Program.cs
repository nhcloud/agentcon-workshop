using DotNetSemanticKernel.Configuration;
using DotNetSemanticKernel.Services;
using Microsoft.SemanticKernel;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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
        var frontendUrl = builder.Configuration["FRONTEND_URL"] ?? "http://localhost:3001";
        policy.WithOrigins(frontendUrl)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add configuration
builder.Services.Configure<AzureAIConfig>(builder.Configuration.GetSection("AzureAI"));

// Add Semantic Kernel services
builder.Services.AddScoped<IKernelBuilder>(provider =>
{
    var config = builder.Configuration.GetSection("AzureAI").Get<AzureAIConfig>();
    var kernelBuilder = Kernel.CreateBuilder();
    
    if (!string.IsNullOrEmpty(config?.AzureOpenAI?.Endpoint) && !string.IsNullOrEmpty(config.AzureOpenAI.ApiKey))
    {
        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: config.AzureOpenAI.ChatDeployment ?? "gpt-4o",
            endpoint: config.AzureOpenAI.Endpoint,
            apiKey: config.AzureOpenAI.ApiKey);
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

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow, framework = ".NET 9" });

var port = Environment.GetEnvironmentVariable("PORT") ?? "8002";
app.Urls.Add($"http://localhost:{port}");

app.Run();