var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

var app = builder.Build();

// Tell app it's hosted under /sampleBallotAdmin
// app.UsePathBase("/sampleBallotAdmin");

//const string pathBase = "/sampleBallotAdmin";
//app.UsePathBase(pathBase);          // <─ sets HttpRequest.PathBase
//app.Use((ctx, next) =>              // optional: redirect naked requests
//{
//    if (ctx.Request.Path == "/")
//        return Task.Run(() =>
//            ctx.Response.Redirect(pathBase + "/"));
//    return next();
//});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();     // for /api/xyz routes
    endpoints.MapRazorPages();      // for .cshtml Razor Pages
});

app.Run();
