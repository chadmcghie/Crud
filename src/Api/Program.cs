using App;
using Infrastructure;

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
                    policy.WithOrigins("http://localhost:50266", "http://127.0.0.1:50266")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });
            builder.Services.AddControllers();            
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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
