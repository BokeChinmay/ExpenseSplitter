using System.Text;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
}

// Add services to the container.

//Database
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddDbContext<AppDbContext>(options =>
    //options.UseNpgsql("Host=localhost;Port=5432;Database=expensesplitter;Username=postgres;Password=postgres"));

//Auth
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
//var jwtSecret = "ThisIsMyTestSecretKeyThatIsAtLeast32Chars!";
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options => {
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
        };
    options.Events = new JwtBearerEvents {
        OnMessageReceived = context => {
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) {
                context.Token = authHeader["Bearer ".Length..].Trim();
            }
            return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

//Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<DebtSimplificationService>();
builder.Services.AddScoped<ReceiptParsingService>();
builder.Services.AddScoped<InsightsService>();

//Swagger - Failed (version control issues?)
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
//     { 
//         Title = "Expense Splitter API", 
//         Version = "v1" 
//     });
//     c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//     {
//         In = Microsoft.OpenApi.Models.ParameterLocation.Header,
//         Description = "Enter: Bearer {token}",
//         Name = "Authorization",
//         Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
//     });
//     c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
//     {
//         {
//             new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//             {
//                 Reference = new Microsoft.OpenApi.Models.OpenApiReference
//                 { 
//                     Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, 
//                     Id = "Bearer" 
//                 }
//             },
//             Array.Empty<string>()
//         }
//     });
// });

// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

builder.Services.AddCors(options => {
    options.AddPolicy("BlazorClient", policy => policy.WithOrigins("http://localhost:5211").AllowAnyHeader().AllowAnyMethod());
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

//Auto migrate on startup
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }

// app.UseSwagger();
// app.UseSwaggerUI();

app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapOpenApi();

app.Run();
