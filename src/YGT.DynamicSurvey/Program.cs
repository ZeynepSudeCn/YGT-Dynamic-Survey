using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using YGT.DynamicSurvey.Data;
using YGT.DynamicSurvey.Models.Identity;
using YGT.DynamicSurvey.Services;

var builder = WebApplication.CreateBuilder(args);

// Windows Event Log, geliştirme ortamında yönetici yetkisi isteyebilir ve
// asıl uygulama hatasını örtebilir. Konsol çıktısı yerel geliştirme için yeterlidir.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, ".data-protection-keys");
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("YGT.DynamicSurvey");


// =====================================================
// DATABASE
// =====================================================

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection"
    );

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection bağlantı bilgisi bulunamadı."
    );
}

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseSqlite(connectionString)
);


// =====================================================
// ASP.NET CORE IDENTITY
// =====================================================

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(
        options =>
        {
            // Şifre kuralları
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = builder.Configuration
                .GetValue("Identity:RequireNonAlphanumeric", true);

            // Aynı e-posta ile birden fazla hesap açılmasın
            options.User.RequireUniqueEmail = true;

            // 5 hatalı girişten sonra 10 dakika kilitle
            options.Lockout.MaxFailedAccessAttempts = 5;

            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(10);

            // E-posta doğrulamasını daha sonra açacağız
            options.SignIn.RequireConfirmedEmail = false;
        }
    )
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<TurkishIdentityErrorDescriber>()
    .AddDefaultTokenProviders();


// =====================================================
// COOKIE AYARLARI
// =====================================================

builder.Services.ConfigureApplicationCookie(
    options =>
    {
        options.LoginPath =
            "/Account/Login";

        options.AccessDeniedPath =
            "/Account/AccessDenied";

        options.ExpireTimeSpan =
            TimeSpan.FromHours(2);

        options.SlidingExpiration =
            true;
    }
);


// =====================================================
// MVC
// =====================================================

builder.Services.AddControllersWithViews();


// =====================================================
// UYGULAMA SERVİSLERİ
// =====================================================

builder.Services.AddScoped<SystemLogService>();

builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<EmailService>();


// =====================================================
// APPLICATION
// =====================================================

var app =
    builder.Build();

var turkishCulture = new System.Globalization.CultureInfo("tr-TR");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = turkishCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = turkishCulture;


// =====================================================
// ADMIN ROLE + ADMIN USER SEED
// =====================================================

using (var scope =
       app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    var roleManager =
        scope.ServiceProvider
            .GetRequiredService<
                RoleManager<IdentityRole>>();

    var userManager =
        scope.ServiceProvider
            .GetRequiredService<
                UserManager<ApplicationUser>>();


    const string adminRoleName =
        "Admin";

    const string superAdminRoleName =
        "SuperAdmin";


    // =================================================
    // ADMIN ROLÜ
    // =================================================

    if (!await roleManager.RoleExistsAsync(
            adminRoleName
        ))
    {
        var roleResult =
            await roleManager.CreateAsync(
                new IdentityRole(
                    adminRoleName
                )
            );

        if (!roleResult.Succeeded)
        {
            var errors =
                string.Join(
                    Environment.NewLine,
                    roleResult.Errors.Select(
                        x => x.Description
                    )
                );

            throw new InvalidOperationException(
                $"Admin rolü oluşturulamadı:{Environment.NewLine}{errors}"
            );
        }
    }


    // =================================================
    // SUPERADMIN ROLÜ
    // =================================================

    if (!await roleManager.RoleExistsAsync(
            superAdminRoleName
        ))
    {
        var roleResult =
            await roleManager.CreateAsync(
                new IdentityRole(
                    superAdminRoleName
                )
            );

        if (!roleResult.Succeeded)
        {
            var errors =
                string.Join(
                    Environment.NewLine,
                    roleResult.Errors.Select(
                        x => x.Description
                    )
                );

            throw new InvalidOperationException(
                $"SuperAdmin rolü oluşturulamadı:{Environment.NewLine}{errors}"
            );
        }
    }


    // =================================================
    // ADMIN SEED BİLGİLERİ
    // =================================================

    var adminEmail =
        app.Configuration[
            "AdminSeed:Email"
        ];

    var adminPassword =
        app.Configuration[
            "AdminSeed:Password"
        ];


    // =================================================
    // ADMIN HESABI OLUŞTUR / ROL VER
    // =================================================

    if (
        !string.IsNullOrWhiteSpace(adminEmail) &&
        !string.IsNullOrWhiteSpace(adminPassword)
    )
    {
        var adminUser =
            await userManager.FindByEmailAsync(
                adminEmail
            );


        if (adminUser is null)
        {
            adminUser =
                new ApplicationUser
                {
                    UserName =
                        adminEmail,

                    Email =
                        adminEmail,

                    FullName =
                        "YGT Yönetici",

                    EmailConfirmed =
                        true,

                    IsActive =
                        true,

                    CreatedAt =
                        DateTime.UtcNow,

                    RequestedAdminAccess =
                        true,

                    AdminRequestStatus =
                        "Approved",

                    AdminReviewedAt =
                        DateTime.UtcNow
                };


            var createResult =
                await userManager.CreateAsync(
                    adminUser,
                    adminPassword
                );


            if (!createResult.Succeeded)
            {
                var errors =
                    string.Join(
                        Environment.NewLine,
                        createResult.Errors.Select(
                            x => x.Description
                        )
                    );

                throw new InvalidOperationException(
                    $"Admin hesabı oluşturulamadı:{Environment.NewLine}{errors}"
                );
            }
        }


        var normalizedAdminEmail =
            adminUser.Email?
                .Trim()
                .ToLowerInvariant();


        // =============================================
        // ygtkku@gmail.com = SUPERADMIN
        // =============================================

        if (
            normalizedAdminEmail ==
            "ygtkku@gmail.com"
        )
        {
            if (!await userManager.IsInRoleAsync(
                    adminUser,
                    superAdminRoleName
                ))
            {
                var addRoleResult =
                    await userManager.AddToRoleAsync(
                        adminUser,
                        superAdminRoleName
                    );


                if (!addRoleResult.Succeeded)
                {
                    var errors =
                        string.Join(
                            Environment.NewLine,
                            addRoleResult.Errors.Select(
                                x => x.Description
                            )
                        );

                    throw new InvalidOperationException(
                        $"SuperAdmin rolü kullanıcıya atanamadı:{Environment.NewLine}{errors}"
                    );
                }
            }
        }

        // =============================================
        // DİĞER SEED HESABI = ADMIN
        // =============================================

        else
        {
            if (!await userManager.IsInRoleAsync(
                    adminUser,
                    adminRoleName
                ))
            {
                var addRoleResult =
                    await userManager.AddToRoleAsync(
                        adminUser,
                        adminRoleName
                    );


                if (!addRoleResult.Succeeded)
                {
                    var errors =
                        string.Join(
                            Environment.NewLine,
                            addRoleResult.Errors.Select(
                                x => x.Description
                            )
                        );

                    throw new InvalidOperationException(
                        $"Admin rolü kullanıcıya atanamadı:{Environment.NewLine}{errors}"
                    );
                }
            }
        }
    }

}

if (app.Configuration.GetValue<bool>("SeedDemoSurveys"))
{
    using var seedScope = app.Services.CreateScope();
    await DemoSurveySeeder.SeedAsync(seedScope.ServiceProvider);
}

if (app.Configuration.GetValue<bool>("PublishCommunityContent"))
{
    using var contentScope = app.Services.CreateScope();
    await CommunityContentSeeder.SeedAndPublishAsync(contentScope.ServiceProvider);
}


// =====================================================
// HTTP PIPELINE
// =====================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Home/Error"
    );

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();


// =====================================================
// ROUTING
// =====================================================

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}"
);


// =====================================================
// START
// =====================================================

app.Run();
