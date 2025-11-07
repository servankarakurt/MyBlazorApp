using Azure.Data.Tables;
using GorevTakipUygulamasi.Areas.Identity;
using GorevTakipUygulamasi.Configuration;
using GorevTakipUygulamasi.Data;
using GorevTakipUygulamasi.Services.TaskServices;
using GorevTakipUygulamasi.Services.ReminderServices;
using GorevTakipUygulamasi.Services.User;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GorevTakipUygulamasi.Models;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. DATABASE CONFIGURATION
// ============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                      throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ============================================================
// 2. IDENTITY CONFIGURATION
// ============================================================
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// ============================================================
// 3. CONFIGURATION SETTINGS
// ============================================================
builder.Services.Configure<AzureStorageSettings>(
    builder.Configuration.GetSection("AzureStorage"));

builder.Services.Configure<LogicAppSettings>(
    builder.Configuration.GetSection("LogicApp"));

builder.Services.Configure<ReminderNotificationSettings>(
    builder.Configuration.GetSection("ReminderNotification"));

// ============================================================
// 4. AZURE TABLE STORAGE
// ============================================================
builder.Services.AddSingleton<TableServiceClient>(serviceProvider =>
{
    var settings = builder.Configuration.GetSection("AzureStorage").Get<AzureStorageSettings>();
    if (string.IsNullOrEmpty(settings?.ConnectionString))
    {
        throw new InvalidOperationException("AzureStorage:ConnectionString bulunamadı!");
    }
    return new TableServiceClient(settings.ConnectionString);
});

// ============================================================
// 5. APPLICATION SERVICES
// ============================================================

// Task Services (GÖREVLER)
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddHttpClient<ITaskLogicAppService, TaskLogicAppService>();
builder.Services.AddScoped<ITaskCompletionService, TaskCompletionService>();

// User Services
builder.Services.AddScoped<IUserService, UserService>();

// Reminder Services (HATIRLATICILAR - Azure Table Storage)
builder.Services.AddScoped<IReminderService, ReminderService>();

// HttpClient (Genel)
builder.Services.AddHttpClient();

// ============================================================
// 6. BLAZOR SERVICES
// ============================================================
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<AuthenticationStateProvider,
    RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

// ============================================================
// 7. BUILD APPLICATION
// ============================================================
var app = builder.Build();

// ============================================================
// 8. AUTOMATIC DATABASE MIGRATION
// ============================================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var pendingMigrations = context.Database.GetPendingMigrations();
        if (pendingMigrations.Any())
        {
            migrationLogger.LogInformation("🔄 Applying {Count} pending migrations...", pendingMigrations.Count());
            context.Database.Migrate();
            migrationLogger.LogInformation("✅ Migrations applied successfully!");
        }
        else
        {
            migrationLogger.LogInformation("✅ Database is up to date.");
        }
    }
    catch (Exception ex)
    {
        var errorLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        errorLogger.LogError(ex, "💥 An error occurred while migrating the database.");
    }
}

// ============================================================
// 9. HTTP PIPELINE CONFIGURATION
// ============================================================
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ============================================================
// 10. MINIMAL API ENDPOINTS (Test & Debug)
// ============================================================

// Test endpoint - Task email gönderme testi
app.MapGet("/api/test/task-email", async (
    HttpContext httpContext,
    ITaskLogicAppService taskLogicAppService,
    UserManager<IdentityUser> userManager,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("🧪 Task email test endpoint called");

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("⚠️ Unauthorized access attempt");
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user?.Email == null)
        {
            logger.LogWarning("⚠️ User email not found for userId: {UserId}", userId);
            return Results.BadRequest(new { error = "User email not found" });
        }

        logger.LogInformation("👤 Sending test email to: {Email}", user.Email);

        var testTask = new TaskItem
        {
            Id = 999,
            Title = "Test Task - Email Kontrolü",
            Description = "Bu bir test e-postasıdır. Sistem düzgün çalışıyor.",
            Status = GorevTakipUygulamasi.Models.TaskStatus.Tamamlandi,
            CompletedDate = DateTime.Now,
            UserId = userId
        };

        var success = await taskLogicAppService.SendTaskCompletionEmailAsync(
            testTask,
            user.Email,
            user.UserName ?? user.Email.Split('@')[0]
        );

        if (success)
        {
            logger.LogInformation("✅ Test email sent successfully");
            return Results.Ok(new
            {
                message = "Test email sent successfully",
                recipient = user.Email,
                taskTitle = testTask.Title
            });
        }
        else
        {
            logger.LogError("❌ Failed to send test email");
            return Results.Problem("Failed to send test email");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "💥 Test email endpoint error");
        return Results.Problem(ex.Message);
    }
}).RequireAuthorization();

// Config kontrol endpoint
app.MapGet("/api/test/config", (IConfiguration config, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("🔍 Config check endpoint called");

        var taskUrl = config["LogicApp:TaskCompletionUrl"];
        var storageConnection = config["AzureStorage:ConnectionString"];
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var result = new
        {
            Environment = environment,
            TaskCompletionUrlConfigured = !string.IsNullOrEmpty(taskUrl),
            TaskUrlLength = taskUrl?.Length ?? 0,
            TaskUrlPreview = !string.IsNullOrEmpty(taskUrl)
                ? taskUrl.Substring(0, Math.Min(50, taskUrl.Length)) + "..."
                : "NOT CONFIGURED",
            AzureStorageConfigured = !string.IsNullOrEmpty(storageConnection),
            StorageConnectionPreview = !string.IsNullOrEmpty(storageConnection)
                ? storageConnection.Substring(0, Math.Min(50, storageConnection.Length)) + "..."
                : "NOT CONFIGURED",
            Timestamp = DateTime.Now
        };

        logger.LogInformation("✅ Config check completed");
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "💥 Config check error");
        return Results.Problem(ex.Message);
    }
}).RequireAuthorization();

// Manuel task completion email gönderme
app.MapPost("/api/test/send-task-completion/{taskId:int}", async (
    int taskId,
    HttpContext httpContext,
    ITaskLogicAppService taskLogicAppService,
    UserManager<IdentityUser> userManager,
    ApplicationDbContext context,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("📤 Manual task completion email request for taskId: {TaskId}", taskId);

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("⚠️ Unauthorized access attempt");
            return Results.Unauthorized();
        }

        var task = await context.TaskItems
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            logger.LogWarning("⚠️ Task not found: {TaskId}", taskId);
            return Results.NotFound(new { error = $"Task {taskId} not found" });
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user?.Email == null)
        {
            logger.LogWarning("⚠️ User email not found for userId: {UserId}", userId);
            return Results.BadRequest(new { error = "User email not found" });
        }

        logger.LogInformation("📧 Sending completion email for task: {Title} to {Email}",
            task.Title, user.Email);

        var success = await taskLogicAppService.SendTaskCompletionEmailAsync(
            task,
            user.Email,
            user.UserName ?? user.Email.Split('@')[0]
        );

        if (success)
        {
            logger.LogInformation("✅ Task completion email sent successfully");
            return Results.Ok(new
            {
                message = $"Task completion email sent for task {taskId}",
                taskTitle = task.Title,
                recipient = user.Email
            });
        }
        else
        {
            logger.LogError("❌ Failed to send task completion email");
            return Results.Problem("Failed to send task completion email");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "💥 Manual task completion email error");
        return Results.Problem(ex.Message);
    }
}).RequireAuthorization();

// ============================================================
// 11. MAP ROUTES
// ============================================================
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// ============================================================
// 12. START APPLICATION
// ============================================================
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 GorevTakipUygulamasi başlatıldı!");
logger.LogInformation("📦 Version: 2.0 (Table Storage Architecture)");
logger.LogInformation("🌍 Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("🔗 URLs: {Urls}", string.Join(", ", app.Urls));

app.Run();