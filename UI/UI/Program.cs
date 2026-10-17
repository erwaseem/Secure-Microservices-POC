using UI.Services;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Session (stores JWT token, username, cart session ID) ────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
});

// -- HttpContextAccessor ------
builder.Services.AddHttpContextAccessor();

// --- API Gateway base URL -------
var gatewayBaseUrl = builder.Configuration["ApiGateway:BaseUrl"]
                  ?? "http://localhost:5020";

// --- ProductCatalogueService (HttpClient → API Gateway → ProductService) ---
builder.Services.AddHttpClient<IProductCatalogueService, ProductCatalogueService>(client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl.TrimEnd('/') + "/");
    client.Timeout     = TimeSpan.FromSeconds(30);
});

// --- CartService (HttpClient → API Gateway → Cart_Services) 
builder.Services.AddHttpClient<ICartService, CartService>(client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl.TrimEnd('/') + "/");
    client.Timeout     = TimeSpan.FromSeconds(30);
});

// --- AuthService (HttpClient → API Gateway → Auth-Service) -------
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl.TrimEnd('/') + "/");
    client.Timeout     = TimeSpan.FromSeconds(30);
});

// ─---─ CommentService (HttpClient → API Gateway → ProductService) ---
builder.Services.AddHttpClient<ICommentService, CommentService>(client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl.TrimEnd('/') + "/");
    client.Timeout     = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// --- Middleware pipeline -------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();   // must come before controllers so Session.Id is available

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
