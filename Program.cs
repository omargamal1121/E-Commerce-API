
using E_Commers.BackgroundJops;
using E_Commers.Context;
using E_Commers.DtoModels;
using E_Commers.Helper;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Repository;
using E_Commers.UOW;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;
using System.Text;

namespace E_Commers
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.AddConsole();
            builder.Services.AddIdentity<Customer, IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
            builder.Services.AddTransient<TokenHelper>();
            builder.Services.AddTransient<ImagesHelper>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
			builder.Services.AddScoped(typeof(IRepository<>), typeof(MainRepository<>));
			builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("Localhost:6379"));
			builder.Services.AddDbContext<AppDbContext>(options=>options.UseSqlServer(builder.Configuration.GetConnectionString("MyConnection")));
			builder.Services.AddControllers().ConfigureApiBehaviorOptions(options=>options.SuppressModelStateInvalidFilter=true);
			builder.Services.AddScoped<CategoryCleanupService>();
			builder.Services.AddHangfire(config => config.UseSqlServerStorage(builder.Configuration.GetConnectionString("MyConnection")));
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
                    ValidIssuer = builder.Configuration["Jwt:Issure"],
					ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]??throw new Exception("Key is missing"))),
                    ValidateLifetime=true,
                    
                };
            }
            );

            var app = builder.Build();
            app.UseHangfireDashboard();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("MyPolicy");
            app.UseMiddleware<SecurityStampMiddleware>();
 


			using (var scope = app.Services.CreateScope())
			{
				var services = scope.ServiceProvider;
				var dbContext = services.GetRequiredService<AppDbContext>();
				dbContext.Database.Migrate();
				await SeedData.SeedDataAsync(services);
				var categoryCleanupService = scope.ServiceProvider.GetRequiredService<CategoryCleanupService>();
				categoryCleanupService.DeleteOldCategories();
                RecurringJob.AddOrUpdate(
                        "Clean-Category",
                        () =>
                        categoryCleanupService.DeleteOldCategories(),
                        Cron.Daily
                );
			}
            app.UseHangfireDashboard("/hangfire");

			app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();
            app.MapControllers();
            app.Run();
        }
    }
}
