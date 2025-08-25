using App;
using Infrastructure;
using static System.Net.Mime.MediaTypeNames;

namespace Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddApplication();
            builder.Services.AddInfrastructureInMemory();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular", policy =>
                {
                    policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200", "https://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddMediatR(services => services.RegisterServicesFromAssembly(typeof(Program).Assembly));

            builder.Services.AddAutoMapper(
                cfg => { },
                typeof(App.DependencyInjection).Assembly, 
                typeof(Infrastructure.DependencyInjection).Assembly
                );

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Global exception handler
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";

                    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                    if (exception?.Error is KeyNotFoundException)
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("{\"error\":\"Resource not found\"}");
                    }
                    else
                    {
                        await context.Response.WriteAsync("{\"error\":\"An error occurred\"}");
                    }
                });
            });

            app.UseHttpsRedirection();
            app.UseCors("AllowAngular");
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
