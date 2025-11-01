using Azure.Data.Tables;
using GorevTakipUygulamasi.Areas.Identity;
using GorevTakipUygulamasi.Configuration;
using GorevTakipUygulamasi.Data;
using GorevTakipUygulamasi.Services;
using GorevTakipUygulamasi.Services.Background;
using GorevTakipUygulamasi.Services.LogicApp;
using GorevTakipUygulamasi.Services.TaskServices;
using GorevTakipUygulamasi.Services.ReminderServices;
using GorevTakipUygulamasi.Services.User;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GorevTakipUygulamasi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                      throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// Configuration Settings
builder.Services.Configure<AzureStorageSettings>(
    builder.Configuration.GetSection("AzureStorage"));

builder.Services.Configure<LogicAppSettings>(
    builder.Configuration.GetSection("LogicApp"));

builder.Services.Configure<ReminderNotificationSettings>(
    builder.Configuration.GetSection("ReminderNotification"));

// Azure Table Storage
builder.Services.AddSingleton<TableServiceClient>(serviceProvider =>
{
    var settings = builder.Configuration.GetSection("AzureStorage").Get<AzureStorageSettings>();
    return new TableServiceClient(settings?.ConnectionString ?? "UseDevelopmentStorage=true");
});

// Task Services
builder.Services.AddScoped<GorevTakipUygulamasi.Services.TaskServices.ITaskService,
                          GorevTakipUygulamasi.Services.TaskServices.TaskService>();

// ⭐ YENİ: TaskLogicAppService - Task completion email için ÖZEL servis
builder.Services.AddHttpClient<ITaskLogicAppService, TaskLogicAppService>();

// Task Completion Service
builder.Services.AddScoped<GorevTakipUygulamasi.Services.TaskServices.ITaskCompletionService,
                          GorevTakipUygulamasi.Services.TaskServices.TaskCompletionService>();

// User Services
builder.Services.AddScoped<GorevTakipUygulamasi.Services.User.IUserService,
                          GorevTakipUygulamasi.Services.User.UserService>();

// Logic App Services (Reminder için)
builder.Services.AddScoped<GorevTakipUygulamasi.Services.LogicApp.ILogicAppService,
                          GorevTakipUygulamasi.Services.LogicApp.LogicAppService>();

// Reminder Services
builder.Services.AddScoped<GorevTakipUygulamasi.Services.ReminderServices.IReminderService,
                          GorevTakipUygulamasi.Services.ReminderServices.ReminderService>();

// Notification Services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ReminderNotificationService>();

// Background Services
builder.Services.AddScoped<GorevTakipUygulamasi.Services.Background.IBackgroundJobService,
                          GorevTakipUygulamasi.Services.Background.BackgroundJobService>();
builder.Services.AddScoped<IReminderCheckService, ReminderCheckService>();

// HttpClient Services
builder.Services.AddHttpClient<GorevTakipUygulamasi.Services.LogicApp.LogicAppService>();
builder.Services.AddHttpClient<ReminderNotificationService>();
builder.Services.AddHttpClient();

// Blazor Services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

var app = builder.Build();

// **OTOMATIK MIGRATION KISMI**
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pendingMigrations = context.Database.GetPendingMigrations();
        if (pendingMigrations.Any())
        {
            Console.WriteLine($"🔄 Applying {pendingMigrations.Count()} pending migrations...");
            context.Database.Migrate();
            Console.WriteLine("✅ Migrations applied successfully!");
        }
        else
        {
            Console.WriteLine("✅ Database is up to date.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Migration error: {ex.Message}");
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline.
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

// ⭐ YENİ: TEST API ENDPOINTS (Minimal API)
app.MapGet("/api/test/task-email", async (
    HttpContext httpContext,
    ITaskLogicAppService taskLogicAppService,
    UserManager<IdentityUser> userManager,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("🧪 Test task email endpoint çağrıldı");

        // Kullanıcı kontrolü
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Unauthorized();
        }

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Json(new { success = false, message = "Kullanıcı ID bulunamadı" }, statusCode: 401);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user?.Email == null)
        {
            return Results.Json(new { success = false, message = "Kullanıcı email adresi bulunamadı" }, statusCode: 400);
        }

        var userName = user.UserName ?? user.Email.Split('@')[0];

        // Test task oluştur
        var testTask = new TaskItem
        {
            Id = 999,
            Title = "🧪 Test Görevi - Email Kontrolü",
            Description = "Bu bir test görevidir. Logic App bağlantısını test etmek için kullanılıyor.",
            UserId = userId,
            Status = GorevTakipUygulamasi.Models.TaskStatus.Tamamlandi,
            CreatedDate = DateTime.Now.AddDays(-7),
            CompletedDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(2)
        };

        logger.LogInformation("📤 Test email gönderiliyor: {Email}", user.Email);

        var success = await taskLogicAppService.SendTaskCompletionEmailAsync(
            testTask,
            user.Email,
            userName
        );

        if (success)
        {
            logger.LogInformation("✅ Test email başarıyla gönderildi!");
            return Results.Json(new
            {
                success = true,
                message = "✅ Test email başarıyla gönderildi!",
                email = user.Email,
                userName = userName,
                taskTitle = testTask.Title,
                timestamp = DateTime.Now,
                note = "Lütfen email adresinizi kontrol edin: " + user.Email
            });
        }
        else
        {
            logger.LogError("❌ Test email gönderilemedi");
            return Results.Json(new
            {
                success = false,
                message = "❌ Email gönderilemedi. Azure Log Stream'den detaylı logları kontrol edin.",
                email = user.Email
            }, statusCode: 400);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "💥 Test email endpoint hatası");
        return Results.Json(new
        {
            success = false,
            message = $"Hata: {ex.Message}",
            type = ex.GetType().Name
        }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapGet("/api/test/config", (IConfiguration config) =>
{
    var taskCompletionUrl = config["LogicApp:TaskCompletionUrl"];
    var hasUrl = !string.IsNullOrEmpty(taskCompletionUrl);

    return Results.Json(new
    {
        hasTaskCompletionUrl = hasUrl,
        urlLength = taskCompletionUrl?.Length ?? 0,
        urlStart = hasUrl ? taskCompletionUrl?.Substring(0, Math.Min(50, taskCompletionUrl!.Length)) : null,
        note = "Güvenlik nedeniyle tam URL gösterilmiyor."
    });
}).RequireAuthorization();

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
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Unauthorized();
        }

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Json(new { success = false, message = "Kullanıcı bulunamadı" }, statusCode: 401);
        }

        var task = await context.TaskItems.FindAsync(taskId);
        if (task == null)
        {
            return Results.Json(new { success = false, message = $"Task bulunamadı: {taskId}" }, statusCode: 404);
        }

        if (task.UserId != userId)
        {
            return Results.Json(new { success = false, message = "Bu task'a erişim yetkiniz yok" }, statusCode: 403);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user?.Email == null)
        {
            return Results.Json(new { success = false, message = "Kullanıcı email adresi bulunamadı" }, statusCode: 400);
        }

        var userName = user.UserName ?? user.Email.Split('@')[0];

        logger.LogInformation("📧 Task completion email gönderiliyor: Task #{TaskId}", taskId);

        var success = await taskLogicAppService.SendTaskCompletionEmailAsync(
            task,
            user.Email,
            userName
        );

        if (success)
        {
            return Results.Json(new
            {
                success = true,
                message = "Email gönderildi",
                taskId = task.Id,
                taskTitle = task.Title,
                email = user.Email
            });
        }
        else
        {
            return Results.Json(new { success = false, message = "Email gönderilemedi" }, statusCode: 400);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "SendCompletionEmail hatası: {TaskId}", taskId);
        return Results.Json(new { success = false, message = ex.Message }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

Console.WriteLine("🚀 GorevTakipUygulamasi başlatıldı!");
Console.WriteLine("🔧 Task Completion Email Service aktif!");
Console.WriteLine("🧪 Test endpoints:");
Console.WriteLine("   - GET  /api/test/task-email");
Console.WriteLine("   - GET  /api/test/config");
Console.WriteLine("   - POST /api/test/send-task-completion/{taskId}");

app.Run();