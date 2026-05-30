using BusinessLayer.IService;
using BusinessLayer.Services;
using DataAccessLayer.DbContexts;
using DataAccessLayer.FileHandlers.FG;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

    var xmlFilename =
      $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

    option.IncludeXmlComments(
        Path.Combine(AppContext.BaseDirectory, xmlFilename));

    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});
builder.Services.AddScoped<BusinessLayer.Mapping.FGMapper>();
builder.Services.AddScoped<IFGImportService,FGImportService>();
builder.Services.AddScoped<IFGReader, FGReader>();
var app = builder.Build();

app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

//app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();