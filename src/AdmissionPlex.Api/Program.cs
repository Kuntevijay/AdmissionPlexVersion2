using AdmissionPlex.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ===== Infrastructure =====
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddAppAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();

// ===== CORS (for Blazor Web) =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
        policy.WithOrigins(
                builder.Configuration["ClientUrl"] ?? "https://localhost:7002",
                "https://localhost:7002",
                "http://localhost:5002",
                "https://localhost:5002"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// ===== Controllers + Swagger =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AdmissionPlex API", Version = "v1" });

    // JWT Bearer auth in Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ===== Pipeline =====
app.UseCustomExceptionHandler();

// Enable Swagger always (restrict in production if needed later)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AdmissionPlex API v1");
    c.RoutePrefix = "swagger";
});

// Redirect root "/" to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseHttpsRedirection();
app.UseCors("BlazorClient");
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ===== Seed on startup =====
await app.SeedDatabaseAsync();

app.Run();
