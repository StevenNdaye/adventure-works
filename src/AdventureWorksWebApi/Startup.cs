using System;
using System.IO;
using AdventureWorksWebApi.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting.Display;
using Serilog.Sinks.RollingFile;
using Swashbuckle.Swagger.Model;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorksWebApi
{
    public class Startup
    {
        private string pathToXmlDocs = "./AdventureWorksWebApi.xml";

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            CurrentEnvironment = env;
        }

        public IConfigurationRoot Configuration { get; }
        private IHostingEnvironment CurrentEnvironment { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            #region [Services Configuration]

            // Create framework services
            services.AddMvc()
                    .AddJsonOptions(opts =>
                    {
                        opts.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                        opts.SerializerSettings.Converters.Add(new StringEnumConverter());
                    });

            
            #endregion

            #region [Dependency Injection]

            #endregion

            #region [Database Configuration]

            // Entity Framework Config
            services.AddDbContext<AdventureWorksDBContext>(options =>
                options.UseSqlServer(Configuration["Data:ConnectionString"]));
            #endregion

            #region [Swagger Configuration]
            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.SingleApiVersion(new Info()
                {
                    Version = "v1",
                    Title = "AdventureWorksWebApi API",
                    Description = "RESTful API to the AdventureWorks Platform"
                });
                options.IncludeXmlComments(pathToXmlDocs);

            });
            #endregion

            #region [Serilog Configuration]
            LogEventLevel eventLevel = LogEventLevel.Information;

            try
            {
                eventLevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), Configuration["Logging:LogLevel"].ToString());
            }
            catch (Exception)
            {
                // The level is not valid, keep the default of Information
            }

            var levelSwitch = new LoggingLevelSwitch()
            {
                MinimumLevel = eventLevel
            };

            var outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] ({RequestId}) {SourceContext} {Message}{NewLine}{Exception}";
            var serilogFormatter = new MessageTemplateTextFormatter(outputTemplate, null);
            var rollingFileSink = new RollingFileSink(Path.Combine(Configuration["Logging:Path"], Configuration["Logging:Log"]), serilogFormatter, 1073741824, 31);

            Log.Logger = new LoggerConfiguration()
                .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore.Storage.Internal.RelationalCommandBuilderFactory"))
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Sink(rollingFileSink)
                .CreateLogger();
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // Use Stackify Prefix profiler http://www.prefix.io/
            if (CurrentEnvironment.EnvironmentName == "PreProd" || CurrentEnvironment.EnvironmentName == "Local")
            {
                app.UseMiddleware<StackifyMiddleware.RequestTracerMiddleware>();
            }

            loggerFactory.AddSerilog();

            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUi();
        }
    }
}
