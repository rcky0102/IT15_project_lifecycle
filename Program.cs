using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// External authentication providers (Google)
builder.Services.AddAuthentication()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    });

// SignalR for real-time messaging
builder.Services.AddSignalR();

// Audit & HTTP context
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Nager.Date holiday API service
builder.Services.AddHttpClient<INagerHolidayService, NagerHolidayService>();

// AWS S3 storage service
builder.Services.AddSingleton<IS3StorageService, S3StorageService>();

// Email sender configuration
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, project_lifecycle.Services.EmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Create a scope to resolve services and seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        // TEMP FIX: remove stale empty migration so MigrateAsync re-applies it with correct columns
        await dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260306135628_AddIsArchivedToMilestoneAndProjectRole'");

        await dbContext.Database.MigrateAsync();
        await DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// SignalR Chat Hub
app.MapHub<project_lifecycle.Hubs.ChatHub>("/chatHub");

app.Run();
