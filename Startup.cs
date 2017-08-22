using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HotelAPI.Models;
using Microsoft.EntityFrameworkCore;
using HotelAPI.Middleware;
using StructureMap;

namespace HotelAPI
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsEnvironment("Development"))
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            //  services.AddApplicationInsightsTelemetry(Configuration);
            services.AddDbContext<HotelContext>(opt => opt.UseInMemoryDatabase());
            
            services.AddMvc();
            //// services.AddSingleton<IHotelRepository, HotelRepository>();
            return ConfigureIoC(services);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            
            app.UseAuthenticateMiddleware();
            app.UseAutherizationMiddleware();
            app.UseMvc();
        }

        public IServiceProvider ConfigureIoC(IServiceCollection services)
        {
            var container = new Container();

            container.Configure(config =>
            {
                // Register stuff in container, using the StructureMap APIs.
                config.Scan(_ =>
                {
                    _.AssemblyContainingType(typeof(Startup));
                    ////  register concrete classes to interfaces where the names match
                    _.WithDefaultConventions();
                    //// automatically register all our implementations of IHotelRepository.
                    _.AddAllTypesOf<IHotelRepository>();
                });

                //Populate the container using the service collection
                config.Populate(services);
            });

            return container.GetInstance<IServiceProvider>();

        }
    }
}
