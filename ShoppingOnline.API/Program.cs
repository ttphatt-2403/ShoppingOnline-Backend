using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using ShoppingOnline.API.Models;
using ShoppingOnline.API.Services;

namespace ShoppingOnline.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Memory Caching for Performance
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<ICacheService, MemoryCacheService>();
            builder.Services.AddScoped<IUserCacheService, UserCacheService>();

            // Thêm DbContext kết nối SQL Server với Performance Optimization
            builder.Services.AddDbContext<ShoppingDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
                       .EnableSensitiveDataLogging(false) // Disable in production
                       .EnableServiceProviderCaching();
            });

            // Đăng ký Services
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

            // Configure CORS for Frontend Integration - SINGLE PORT APPROACH
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:3000",    // Primary frontend port
                        "https://localhost:3000",   // HTTPS version
                        "https://yourdomain.com",   // Production domain
                        "https://www.yourdomain.com" // Production www
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                });

                // For development - allow all origins (use carefully)
                options.AddPolicy("Development", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Cấu hình JWT Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? 
                                throw new InvalidOperationException("JWT Key is not configured")))
                    };
                });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "Shopping Online API", 
                    Version = "v1",
                    Description = "ASP.NET Core Web API for Shopping Online System"
                });
                
                // Cấu hình JWT cho Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            // Ensure database is created and Shipping table exists
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ShoppingDbContext>();
                try
                {
                    // Create Shipping table if it doesn't exist
                    context.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Shippings' AND xtype='U')
                        BEGIN
                            CREATE TABLE [dbo].[Shippings] (
                                [ShippingId] int IDENTITY(1,1) NOT NULL,
                                [OrderId] int NULL,
                                [ShipperId] int NULL,
                                [ShippingAddress] nvarchar(255) NULL,
                                [ShippingDate] datetime NULL,
                                [DeliveryDate] datetime NULL,
                                [Status] nvarchar(20) NULL DEFAULT 'Preparing',
                                CONSTRAINT [PK_Shippings] PRIMARY KEY ([ShippingId]),
                                CONSTRAINT [FK_Shippings_Orders] FOREIGN KEY ([OrderId]) REFERENCES [Orders]([order_id]),
                                CONSTRAINT [FK_Shippings_Users] FOREIGN KEY ([ShipperId]) REFERENCES [Users]([user_id])
                            );
                        END
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating Shipping table: {ex.Message}");
                }
            }

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                // Enable static files (for images, etc.)
                app.UseStaticFiles();
                app.UseHttpsRedirection();

            // Enable CORS - MUST be before Authentication
            if (app.Environment.IsDevelopment())
            {
                app.UseCors("Development"); // Allow all for development
            }
            else
            {
                app.UseCors("AllowFrontend"); // Specific origins for production
            }

            // Authentication & Authorization phải đứng trước MapControllers
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
