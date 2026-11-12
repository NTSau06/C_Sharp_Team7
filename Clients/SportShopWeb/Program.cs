using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using SportShopWeb.DAL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(2);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.Name = "SportShop.Session";
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.AccessDeniedPath = "/Account/Login";
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = true;
        o.Cookie.Name = "SportShop.Auth";
    });

var app = builder.Build();

// ---------------------------------------------------------------- CSDL dùng chung với phân hệ Admin
// Tự dò máy chủ SQL Server và tự tạo CSDL SportShopDB (11 bảng + dữ liệu mẫu) nếu chưa có.
// Chạy NỀN để website mở ngay; trong lúc chờ, khách thấy trang "Đang chuẩn bị cơ sở dữ liệu".
string connectionString = builder.Configuration.GetConnectionString("SportShopDB");
DBConnect.ConnectionString = connectionString;
bool autoDetect = builder.Configuration.GetValue("Database:AutoDetectServer", true);
bool autoCreate = builder.Configuration.GetValue("Database:AutoCreate", true);
_ = Task.Run(() =>
{
    try
    {
        DBConnect.ConnectionString = DatabaseInitializer.Prepare(connectionString, autoDetect, autoCreate, msg =>
        {
            DbStatus.Message = msg;
            app.Logger.LogInformation("{Message}", msg);
        });
        DbStatus.Ready = true;
        app.Logger.LogInformation("CSDL SportShopDB đã sẵn sàng.");
    }
    catch (Exception ex)
    {
        DbStatus.Error = ex.Message;
        app.Logger.LogError("Không chuẩn bị được CSDL SportShopDB: {Message}", ex.Message);
    }
});

// ---------------------------------------------------------------- pipeline
// Luôn dùng trang lỗi thân thiện (tiếng Việt) cho khách hàng
app.UseExceptionHandler("/Home/Error");
if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}");

app.UseStaticFiles();

// Chưa có CSDL => trả về trang chờ (tự tải lại sau 3 giây) hoặc trang báo lỗi kèm hướng dẫn
app.Use(async (context, next) =>
{
    if (DbStatus.Ready)
    {
        await next();
        return;
    }
    string err = DbStatus.Error;
    string body = err == null
        ? "<h1>Đang chuẩn bị cơ sở dữ liệu...</h1><p>" + System.Net.WebUtility.HtmlEncode(DbStatus.Message) +
          "</p><p class=m>Lần chạy đầu có thể mất 20–40 giây (dò SQL Server và tạo CSDL SportShopDB). Trang sẽ tự tải lại.</p>"
        : "<h1>Không kết nối được SQL Server</h1><p>" + System.Net.WebUtility.HtmlEncode(err) + "</p>" +
          "<ul><li>Cài <b>SQL Server Express LocalDB</b>: Visual Studio Installer → Modify → Individual components.</li>" +
          "<li>Hoặc sửa <code>Data Source</code> trong <code>appsettings.json</code> cho đúng tên máy chủ, rồi chạy lại (F5).</li></ul>";
    context.Response.StatusCode = 503;
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(
        "<!DOCTYPE html><html lang=vi><head><meta charset=utf-8><title>Sport Shop</title>" +
        (err == null ? "<meta http-equiv=refresh content=3>" : "") +
        "<style>body{font-family:Segoe UI,Arial,sans-serif;background:#f4f6f8;display:grid;place-items:center;min-height:90vh;margin:0}" +
        "div{background:#fff;padding:32px 40px;border-radius:14px;box-shadow:0 4px 18px rgba(0,0,0,.08);max-width:640px}" +
        "h1{color:#00795a;font-size:1.4rem}.m{color:#6b7785}code{background:#eef2f5;padding:1px 6px;border-radius:5px}</style></head>" +
        "<body><div>" + body + "</div></body></html>");
});

app.UseRouting();

var vi = new CultureInfo("vi-VN");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(vi),
    SupportedCultures = new[] { vi },
    SupportedUICultures = new[] { vi }
});

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
