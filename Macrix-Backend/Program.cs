using Macrix_Backend.Models;
using Microsoft.AspNetCore.Authentication.Negotiate;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<Macrix_Backend.Models.MyDbContext>();
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


//builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
//   .AddNegotiate();

//builder.Services.AddAuthorization(options =>
//{
//    // By default, all incoming requests will be authorized according to the default policy.
//    options.FallbackPolicy = options.DefaultPolicy;
//});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

// app.UseAuthorization();

app.MapControllers();

app.Run();
