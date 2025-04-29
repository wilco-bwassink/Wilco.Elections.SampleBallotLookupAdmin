var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}


if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseDeveloperExceptionPage();

app.UseEndpoints(endpoints =>
{
	endpoints.MapControllers(); // Optional — for attribute-routed API controllers
	endpoints.MapRazorPages();  // ✅ This is what enables Razor Pages like your delete-file page
});

app.Run(); // ✅ This should be last


