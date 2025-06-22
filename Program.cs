using E_Commers.BackgroundJops;
using E_Commers.Context;
using E_Commers.DtoModels;
using E_Commers.DtoModels.Responses;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Mappings;
using E_Commers.Middleware;
using E_Commers.Models;
using E_Commers.Repository;
using E_Commers.Services;
using E_Commers.Services.AccountServices;
using E_Commers.Services.AdminOpreationServices;
using E_Commers.Services.Cache;
using E_Commers.Services.Category;
using E_Commers.Services.EmailServices;
using E_Commers.Services.Product;
using E_Commers.Services.ProductInventoryServices;
using E_Commers.Services.WareHouseServices;
using E_Commers.UOW;
using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MySqlConnector;
using Newtonsoft.Json;
using Scalar.AspNetCore;
using Serilog;
using Serilog;
using Serilog.AspNetCore;
using StackExchange.Redis;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
namespace E_Commers
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder
                .Services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling =
                        ReferenceLoopHandling.Serialize;
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                })
                .ConfigureApiBehaviorOptions(options =>
                    options.SuppressModelStateInvalidFilter = true
                );
			Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
			 .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
			 .CreateLogger();

			builder.Host.UseSerilog();
			builder.Services.AddTransient<ICategoryLinkBuilder, CategoryLinkBuilder>();
            builder.Services.AddTransient<IProductLinkBuilder, ProductLinkBuilder>();
            builder.Services.AddTransient<IProductInventoryLinkBuilder, ProductInventoryLinkBuilder>();
			builder.Services.AddTransient<IAccountLinkBuilder, AccountLinkBuilder>();
			builder.Services.AddTransient<IWareHouseLinkBuilder, WareHouseLinkBuilder>();
            builder
                .Services.AddIdentity<Customer, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            builder.Services.AddTransient<IImagesServices, ImagesServices>();
            builder.Services.AddTransient<IErrorNotificationService, ErrorNotificationService>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<ICategoryServices, CategoryServices>();
            builder.Services.AddScoped<IWareHouseRepository, WareHouseRepository>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IProductInventoryRepository, ProductInventoryRepository>();
            builder.Services.AddScoped<IAdminOpreationServices, AdminOpreationServices>();
            builder.Services.AddScoped<IWareHouseServices, WareHouseServices>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped(typeof(IRepository<>), typeof(MainRepository<>));
            builder.Services.AddScoped<IAccountServices, AccountServices>();
            builder.Services.AddScoped<IProductsServices, ProductsServices>();
            builder.Services.AddScoped<IProductInventoryService, Services.ProductInventoryServices.ProductInventoryService>();
            builder.Services.AddAutoMapper(typeof(MappingProfile));
			builder.Services.AddTransient<IEmailSender, EmailSender>();
			builder.Services.AddScoped<ErrorNotificationService>();
			builder.Services.AddScoped<CategoryCleanupService>();
            builder.Services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect("Localhost:6379")
            );
            builder.Services.AddSingleton<ICacheManager, CacheManager>();

            builder.Services.AddDbContext<AppDbContext>(
                (provider, options) =>
                {
                    options.UseMySql(
                        builder.Configuration.GetConnectionString("MyConnectionMySql"),
                        new MySqlServerVersion(new Version(8, 0, 21))
                    );
                }
            );
            builder.Services.AddHangfire(config =>
                config.UseStorage(
                    new MySqlStorage(
                        builder.Configuration.GetConnectionString("MyConnectionMySql"),
                        new MySqlStorageOptions
                        {
                            TablesPrefix = "Hangfire_",
                            QueuePollInterval = TimeSpan.FromSeconds(10),
                        }
                    )
                )
            );
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    "MyPolicy",
                    Options =>
                    {
                        Options.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
                    }
                );
            });
            builder.Services.AddRateLimiter(async options =>
            {
                
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            AutoReplenishment = true
                        }));

                options.AddPolicy("login", context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 6,
                            SegmentsPerWindow= 3,
                            Window = TimeSpan.FromMinutes(1),
                            AutoReplenishment = true
                        }));

             
                options.AddPolicy("register", context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 6,
                            SegmentsPerWindow = 3,
							Window = TimeSpan.FromMinutes(1),
                            AutoReplenishment = true
                        }));

                options.AddPolicy("reset", context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 6,
                            SegmentsPerWindow = 3,
							Window = TimeSpan.FromMinutes(1),
                            AutoReplenishment = true
                        }));
                options.OnRejected= async (context,token) =>
                {
                    context.HttpContext.Response.StatusCode = 429; // Too Many Request
                    context.HttpContext.Response.ContentType = "application/json";

                    var response = ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Requests","Too many request"),429);
                    await context.HttpContext.Response.WriteAsync(
                        JsonConvert.SerializeObject(response),
                        token
                    );

				};
			});
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter JWT Bearer token",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                };

                c.AddSecurityDefinition("Bearer", securityScheme);

                var securityRequirement = new OpenApiSecurityRequirement
                {
                    { securityScheme, new[] { "Bearer" } },
                };

                c.AddSecurityRequirement(securityRequirement);
            });
            builder
                .Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true; 
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                builder.Configuration["Jwt:Key"]
                                    ?? throw new Exception("Key is missing")
                            )
                        ),
                        ValidateLifetime = true,
                    };
                });

            var app = builder.Build();
            app.UseCors("MyPolicy");
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseHangfireDashboard("/hangfire");

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var dbContext = services.GetRequiredService<AppDbContext>();

                dbContext.Database.Migrate();
                await DataSeeder.SeedDataAsync(services);
                var categoryCleanupService =
                    scope.ServiceProvider.GetRequiredService<CategoryCleanupService>();
                RecurringJob.AddOrUpdate(
                    "Clean-Category",
                    () => categoryCleanupService.DeleteOldCategories(),
                    Cron.Daily
                );
            }

            app.UseAuthentication();
            app.UseUserAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<SecurityStampMiddleware>();
            app.UseRateLimiter();
            app.UseResponseCaching();
            app.MapControllers();
            app.Run();
        }
    }
}
