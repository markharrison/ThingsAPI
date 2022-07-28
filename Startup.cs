using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;


using ThingsAPI.Services;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace ThingsAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            config = configuration;
        }

        public IConfiguration config { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<ThingService>(new ThingService(config));
            services.AddControllers();
            services.AddCors();
            services.AddSignalR();
            services.AddSwaggerGen(c =>
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

                string strURL = config.GetValue<string>("ServerURL");

            });
            services.AddApplicationInsightsTelemetry();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ThingService ts )
        {
            if (env.IsDevelopment())
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

                    if (httpRequest.Headers["x-forwarded-proto"].Count == 0) 
                    {
                        swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"{httpRequest.Scheme}://{httpRequest.Host.Value}{basePath}" } };
                    }
                    else
                    {
                        var scheme = httpRequest.Headers["x-forwarded-proto"].ToString();
                        var host = httpRequest.Headers["x-forwarded-host"].ToString();
                        var port = httpRequest.Headers["x-forwarded-port"].ToString();
                        swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"{scheme}://{host}:{port}{basePath}" } };
                    }

                });
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mark Harrison Things API V1");
                c.RoutePrefix = string.Empty;
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<NotifyHub>("/NotifyHub");
                endpoints.MapGet("/appconfiginfo", async context => await context.Response.WriteAsync(ts.GetAppConfigInfo(context)));
                endpoints.MapControllers();
            });
        }
    }
}
