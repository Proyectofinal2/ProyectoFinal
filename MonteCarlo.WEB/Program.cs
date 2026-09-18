using MonteCarlo.WEB.Services;
using MonteCarlo.WEB.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Agregar autenticación por cookies
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/admin/login";
        options.LogoutPath = "/admin/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Registrar IHttpContextAccessor para que los servicios accedan al contexto HTTP
builder.Services.AddHttpContextAccessor();

// Registrar el handler que agrega Bearer token automáticamente
builder.Services.AddScoped<AuthTokenHandler>();

// Registrar AuthApiService y UsuariosApiService (por su interfaz) con el
// HttpClient tipado y el handler de autenticación.
builder.Services
    .AddHttpClient<IAuthApiService, AuthApiService>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ApiSettings:BaseUrl"]!);
    })
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services
    .AddHttpClient<IUsuariosApiService, UsuariosApiService>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ApiSettings:BaseUrl"]!);
    })
    .AddHttpMessageHandler<AuthTokenHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Panel}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
