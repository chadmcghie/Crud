using App;
using Infrastructure;
using FluentValidation;
using FluentValidation.AspNetCore;
using Api.Validators;
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
            
            // Configure FluentValidation
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<CreatePersonRequestValidator>();
            
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

            app.UseHttpsRedirection();
            app.UseCors("AllowAngular");
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
