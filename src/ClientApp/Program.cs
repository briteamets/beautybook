using ClientApp.Services;
using ClientApp.Filters;
using ClientApp.ModelBinders;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization();

var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("et") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders =
    [
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider()
    ];
});

builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add<ClientApiExceptionFilter>();
        options.ModelBinderProviders.Insert(0, new FlexibleDecimalModelBinderProvider());
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();
builder.Services.AddHttpClient<BackendApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5065");
});
builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<ProvidersApiService>();
builder.Services.AddScoped<ServicesApiService>();
builder.Services.AddScoped<CategoriesApiService>();
builder.Services.AddScoped<BookingsApiService>();
builder.Services.AddScoped<ReviewsApiService>();
builder.Services.AddScoped<AvailabilityApiService>();
builder.Services.AddScoped<ServiceTypesApiService>();
builder.Services.AddScoped<FavoritesApiService>();
builder.Services.AddScoped<NotificationsApiService>();
builder.Services.AddScoped<PaymentsApiService>();
builder.Services.AddScoped<SalonsApiService>();
builder.Services.AddScoped<ScheduleApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
