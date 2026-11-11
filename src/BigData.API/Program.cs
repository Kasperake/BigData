using BigData.API.ExceptionHandler;
using BigData.Application;
using BigData.Infrastructure.Persistence;
using BigData.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<WeatherDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("ConnectionString is missing")));

builder.Services.AddScoped<IWeatherObservationReadRepository, WeatherObservationReadRepository>();

builder.Services.AddCqrs(options => options.RegisterHandlersFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Instance ??=
            $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}");

builder.Services.AddExceptionHandler<WeatherExceptionHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
