using Macrix_Backend.Models;
using Microsoft.AspNetCore.Authentication.Negotiate;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Singleton - for better performance
builder.Services.AddDbContext<Macrix_Backend.Models.MyDbContext>(ServiceLifetime.Singleton);
builder.Services.AddSingleton<IPersonsRepository, PersonsRepository>();

builder.Services.AddApiVersioning(opt =>
{
    opt.ReportApiVersions = true;
    opt.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ApiVersionReader = Microsoft.AspNetCore.Mvc.Versioning.ApiVersionReader.Combine(
        new Microsoft.AspNetCore.Mvc.Versioning.HeaderApiVersionReader(),
        new Microsoft.AspNetCore.Mvc.Versioning.QueryStringApiVersionReader(),
        new Microsoft.AspNetCore.Mvc.Versioning.QueryStringApiVersionReader("ver"),
        new Microsoft.AspNetCore.Mvc.Versioning.UrlSegmentApiVersionReader()
        );
});


#if false
// for simple test project, authorization can be switched-off

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});
#endif 

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

// app.UseAuthorization();

app.MapControllers();

app.Run();
