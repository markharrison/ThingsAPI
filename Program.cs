
using Microsoft.AspNetCore.Cors.Infrastructure;
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

            ThingService cs = new ThingService(builder.Configuration);
            builder.Services.AddSingleton(cs);

            builder.Services.AddControllers();
            builder.Services.AddCors();
            builder.Services.AddSignalR();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "3.0.1",
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
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder => builder
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed((host) => true)
                .AllowCredentials()
);

            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swaggerDoc, httpRequest) =>
                {
                    var basePath = "/";
                    var host = httpRequest.Host.Value;
                    var scheme = (httpRequest.IsHttps || httpRequest.Headers["x-forwarded-proto"].ToString() == "https") ? "https" : "http";

                    if (httpRequest.Headers["x-forwarded-host"].ToString() != "")
                    {
                        host = httpRequest.Headers["x-forwarded-host"].ToString() + ":" + httpRequest.Headers["x-forwarded-port"].ToString();
                    }

                    swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"{scheme}://{host}{basePath}" } };

                });
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mark Harrison Things API V1");
                c.RoutePrefix = string.Empty;
            });

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapHub<NotifyHub>("/NotifyHub");
            app.MapGet("/appconfiginfo", async context => await context.Response.WriteAsync(cs.GetAppConfigInfo(context)));

            app.MapControllers();

            app.Run();
        }
    }
}
