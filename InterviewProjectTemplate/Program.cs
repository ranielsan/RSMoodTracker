
using InterviewProjectTemplate.Data;
using InterviewProjectTemplate.Services;
using Microsoft.EntityFrameworkCore;

namespace InterviewProjectTemplate
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration
                .GetConnectionString("MySQLConnectionString")
                ?? throw new InvalidOperationException(
                "MySQLConnectionString is not configured.");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySQL(connectionString));
            // Add services to the container.
            builder.Services.AddCors(o => o.AddDefaultPolicy(builder =>
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader()));

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddScoped<IMoodService, MoodService>();
            builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

            var app = builder.Build();

            await using (var scope = app.Services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                await DatabaseInitializer.InitializeAsync(context);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors();

            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
