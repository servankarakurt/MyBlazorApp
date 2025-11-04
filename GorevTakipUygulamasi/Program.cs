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

// Azure Table Storage (YENİ MİMARİ İÇİN KULLANILACAK)
builder.Services.AddSingleton<TableServiceClient>(serviceProvider =>
{
    var settings = builder.Configuration.GetSection("AzureStorage").Get<AzureStorageSettings>();
    if (string.IsNullOrEmpty(settings?.ConnectionString))
    {
        throw new InvalidOperationException("AzureStorage:ConnectionString bulunamadı!");
    }
    return new TableServiceClient(settings.ConnectionString);
});

// Task Services (GÖREVLER İÇİN KULLANILACAK)
builder.Services.AddScoped<GorevTakipUygulamasi.Services.TaskServices.ITaskService,
                          GorevTakipUygulamasi.Services.TaskServices.TaskService>();

// Task completion email için ÖZEL servis (GÖREVLER İÇİN KULLANILACAK)
builder.Services.AddHttpClient<ITaskLogicAppService, TaskLogicAppService>();

// Task Completion Service (GÖREVLER İÇİN KULLANILACAK)
builder.Services.AddScoped<GorevTakipUygulamasi.Services.TaskServices.ITaskCompletionService,
                          GorevTakipUygulamasi.Services.TaskServices.TaskCompletionService>();

// User Services (KULLANILACAK)
builder.Services.AddScoped<GorevTakipUygulamasi.Services.User.IUserService,
                          GorevTakipUygulamasi.Services.User.UserService>();

// Reminder Services (YENİ MİMARİ - AZURE TABLE VERSİYONU KULLANILACAK)
builder.Services.AddScoped<GorevTakipUygulamasi.Services.ReminderServices.IReminderService,
                          GorevTakipUygulamasi.Services.ReminderServices.ReminderService>();

// HttpClient Services
builder.Services.AddHttpClient(); // Bu genel olan kalsın

// Blazor Services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

// ----- UYGULAMANIN ÇALIŞMASINI SAĞLAYAN EKSİK KISIM -----
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
            context.Database.Migrate(); // Migration'ı (RemovedReminderFromSQL) burada uygulayacak
            Console.WriteLine("✅ Migrations applied successfully!");
        }
        else
        {
            Console.WriteLine("✅ Database is up to date.");
        }
    }
    catch (Exception ex)
    {
        // Başlangıçta migration hatası olursa logla
        Console.WriteLine($"💥 Migration error: {ex.Message}");
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        // Hata olsa bile uygulamanın çökmesini engelleme (belki DB geçici kapalıdır)
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

// --- Minimal API'ler (bunlar sendeydi, geri ekledim) ---
app.MapGet("/api/test/task-email", async (
    HttpContext httpContext,
    ITaskLogicAppService taskLogicAppService,
    UserManager<IdentityUser> userManager,
    ILogger<Program> logger) =>
{
    // ... (Test endpoint kodların) ...
}).RequireAuthorization();

app.MapGet("/api/test/config", (IConfiguration config) =>
{
    // ... (Test endpoint kodların) ...
}).RequireAuthorization();

app.MapPost("/api/test/send-task-completion/{taskId:int}", async (
    int taskId,
    HttpContext httpContext,
    ITaskLogicAppService taskLogicAppService,
    UserManager<IdentityUser> userManager,
    ApplicationDbContext context,
    ILogger<Program> logger) =>
{
    // ... (Test endpoint kodların) ...
}).RequireAuthorization();
// --- Minimal API'ler sonu ---

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

Console.WriteLine("🚀 GorevTakipUygulamasi başlatıldı! (v2 - Table Storage Mimarisi)");

app.Run(); // <-- UYGULAMAYI ÇALIŞTIRAN EN ÖNEMLİ KOMUT