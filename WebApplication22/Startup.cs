using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using WebDocLoader.Filters;
using WebDocLoader.Security;

namespace WebDocLoader
{
    public class Startup
    {
        
        private IWebHostEnvironment CurrentEnvironment{ get; } 
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            OracleConfiguration.BindByName = true;

            OracleConfiguration.OracleDataSources.Add("autonomous",
                Configuration["ConnectionString:tnsDescriptor"]);

            // строка подключения
            services.AddSingleton(new OracleConnectionStringBuilder
            {
                DataSource = "autonomous",
                UserID = Configuration["ConnectionString:UserID"],
                Password = Configuration["ConnectionString:Password"],
                Pooling = true,
                MaxPoolSize = 10,
                ConnectionLifeTime = 120,
                ConnectionTimeout = 60
            });

            services.AddTransient(provider =>
            {
                var builder = provider.GetRequiredService<OracleConnectionStringBuilder>();
                return new OracleConnection(builder.ConnectionString);
            });

            services.AddControllers(options =>
            {
                //options.Filters.Add(typeof(ErrorHandlingFilter));
            });

            services.AddSingleton<ITokenService, TokenService>();
            services.UseTokenAuthentication();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var options = new DefaultFilesOptions();
            options.DefaultFileNames.Clear();
            options.DefaultFileNames.Add("index.html");
            app.UseDefaultFiles(options);
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
