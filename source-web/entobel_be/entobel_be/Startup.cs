using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using entobel_be.Services;

namespace entobel_be
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options => {
                options.AddPolicy("mypolicy", builder => builder
                 .AllowAnyOrigin()
                 .AllowAnyMethod()
                 .AllowAnyHeader());
            });
            services.AddControllers();
            // register Services class in dependency injection container
            services.AddSingleton<UserService>();
            services.AddSingleton<DbService>();
            services.AddSingleton<AdsService>();
            services.AddSingleton<ReportService>();
            services.AddSingleton<MailService>();
            // register MonitoringService as IHostedService
            services.AddSingleton<BgService>();
            services.AddSingleton<IHostedService, BgService>(
                serviceProvider => serviceProvider.GetService<BgService>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("mypolicy");

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
