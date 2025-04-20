
using E_Commers.BackgroundJops;
using E_Commers.Context;
using E_Commers.DtoModels;
using E_Commers.Services;
using E_Commers.Interceptors;
using E_Commers.Interfaces;
using E_Commers.Intersctors;
using E_Commers.Models;
using E_Commers.Repository;
using E_Commers.UOW;
using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MySqlConnector;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;
using System.Text;
using System.Text.Json.Serialization;

namespace E_Commers
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers().AddJsonOptions(jo=>
            { jo.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                jo.JsonSerializerOptions.WriteIndented=true;
            });
            builder.Logging.AddConsole();//pirnt log in console
            builder.Services.AddResponseCaching();// for caching response of method of controller
            builder.Services.AddIdentity<Customer, IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();// for identity
            builder.Services.AddScoped<ITokenService,TokenService>();
            builder.Services.AddScoped<IRefreshTokenService,RefreshTokenService>();
            builder.Services.AddTransient<IImagesServices,ImagesServices>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IWareHouseRepository, WareHouseRepository>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
			builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			builder.Services.AddScoped(typeof(IRepository<>), typeof(MainRepository<>));
			builder.Services.AddScoped<SoftDeleteInterceptor>();
			builder.Services.AddScoped<AddOpreationInDbInterceptor>();
			builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("Localhost:6379"));
            builder.Services.AddDbContext<AppDbContext>((provider,options) =>
           { 
               
               options.UseMySql(builder.Configuration.GetConnectionString("MyConnectionMySql"), new MySqlServerVersion(new Version(8, 0, 21))).AddInterceptors(provider.GetRequiredService<SoftDeleteInterceptor>(), provider.GetRequiredService<AddOpreationInDbInterceptor>());
           });
			builder.Services.AddControllers().ConfigureApiBehaviorOptions(options=>options.SuppressModelStateInvalidFilter=true);
			builder.Services.AddScoped<CategoryCleanupService>();
			builder.Services.AddHangfire(config => config.UseStorage(new MySqlStorage(builder.Configuration.GetConnectionString("MyConnectionMySql"),new MySqlStorageOptions 
            {
				TablesPrefix = "Hangfire_", 
				QueuePollInterval = TimeSpan.FromSeconds(10) 
			})));
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("MyPolicy", Options =>
                {
                    Options.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
                });
            });
            builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("Limiter", options =>
            {
                options.Window = TimeSpan.FromMinutes(1);
                options.PermitLimit = 10;
            }) );
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
						Id = "Bearer"
					}
				};

				c.AddSecurityDefinition("Bearer", securityScheme);

				var securityRequirement = new OpenApiSecurityRequirement
	{
		{ securityScheme, new[] { "Bearer" } }
	};

				c.AddSecurityRequirement(securityRequirement);
			});
			builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.SaveToken = true;//save token in httpcontext
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
					ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]??throw new Exception("Key is missing"))),
                    ValidateLifetime=true,
                    
                };
            }
            );

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
				var categoryCleanupService = scope.ServiceProvider.GetRequiredService<CategoryCleanupService>();
                RecurringJob.AddOrUpdate(
                        "Clean-Category",
                        () =>
                        categoryCleanupService.DeleteOldCategories(),
                        Cron.Daily
                );
			}
          


			app.UseAuthentication();
			app.UseAuthorization();
			app.UseMiddleware<SecurityStampMiddleware>();
            app.UseRateLimiter();
            app.UseResponseCaching();
            app.MapControllers();
            app.Run();
        }
    }
}
