using khidma_backend.Data;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // EF Core DbContext registration
        var connectionString = builder.Configuration.GetConnectionString("KhidmaDbContext") 
            ?? builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        // CORS for GitHub Pages frontend
        const string CorsPolicyName = "FrontendPolicy";
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: CorsPolicyName, policy =>
            {
                policy.WithOrigins("https://<your-username>.github.io")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseCors(CorsPolicyName);

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
