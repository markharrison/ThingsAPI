
using Microsoft.OpenApi.Models;
using ThingsAPI.Services;

namespace ThingsAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<ThingService>(new ThingService(builder.Configuration));
            builder.Services.AddControllers();
            builder.Services.AddCors();
            builder.Services.AddSignalR();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Mark Harrison Things API",
                    Description = "Things API",
                    TermsOfService = new Uri("https://github.com/markharrison/ThingsAPI/blob/master/LICENSE"),
                    Contact = new OpenApiContact
                    {
                        Name = "Mark Harrison",
                        Email = "mark.thingsapi@harrison.ws",
                        Url = new Uri("https://github.com/markharrison/ThingsAPI"),
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Use under MIT License",
                        Url = new Uri("https://github.com/markharrison/ThingsAPI/blob/master/LICENSE"),
                    }
                });

                c.EnableAnnotations();

            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
