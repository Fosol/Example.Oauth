using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using Example.Oauth.Server.Models;
using Microsoft.Data.Entity;
using Microsoft.AspNet.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.AspNet.Authentication.Cookies;
using Example.Oauth.Server.Providers;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity;

namespace Example.Oauth.Server
{
    public class Startup
    {
        #region Properties
        /// <summary>
        /// get/set - Configuration settings for the application.
        /// </summary>
        public IConfiguration Configuration { get; }

        public IConfigurationSection DataSection { get; }
        #endregion

        public Startup(IHostingEnvironment env, IApplicationEnvironment appEnv)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(appEnv.ApplicationBasePath)
                .AddJsonFile("config.json")
                .AddJsonFile($"config.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            this.Configuration = builder.Build();
            this.DataSection = this.Configuration.GetSection("Data");
        }

        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEntityFramework()
                .AddSqlServer()
                .AddInMemoryDatabase()
                .AddDbContext<ApplicationContext>(options => {
                    options.UseInMemoryDatabase();
                })
                .AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(Configuration["Data:DefaultConnection:ConnectionString"]));

            // Add Identity services to the services container.
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                //options.Cookies.ApplicationCookieAuthenticationScheme = "ServerCookie";
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<SharedAuthenticationOptions>(options => {
                options.SignInScheme = "ServerCookie";
            });

            services.AddOpenIdConnectServer();

            services.AddAuthentication();
            services.AddCaching();
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment hostEnv, ILoggerFactory loggerFactory)
        {
            loggerFactory.MinimumLevel = LogLevel.Information;
            loggerFactory.AddConsole();
            loggerFactory.AddDebug();

            app.UseIISPlatformHandler();

            if (hostEnv.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("Error");
            }

            // Create a new branch where the registered middleware will be executed only for API calls.
            app.UseWhen(context => context.Request.Path.StartsWithSegments(new PathString("/api")), branch =>
            {
                branch.UseJwtBearerAuthentication(options =>
                {
                    options.AutomaticAuthenticate = true;
                    options.AutomaticChallenge = true;
                    options.RequireHttpsMetadata = false;

                    options.Audience = "http://localhost:54683/";
                    options.Authority = "http://localhost:54683/";
                });
            });

            // Create a new branch where the registered middleware will be executed only for non API calls.
            app.UseWhen(context => !context.Request.Path.StartsWithSegments(new PathString("/api")), branch =>
            {
                // Insert a new cookies middleware in the pipeline to store
                // the user identity returned by the external identity provider.
                branch.UseCookieAuthentication(options =>
                {
                    options.AutomaticAuthenticate = true;
                    options.AutomaticChallenge = true;
                    options.AuthenticationScheme = "ServerCookie";
                    options.CookieName = CookieAuthenticationDefaults.CookiePrefix + "ServerCookie";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                    options.LoginPath = new PathString("/account/login");
                });

                branch.UseIdentity();
            });

            app.UseOpenIdConnectServer(options =>
            {
                options.Provider = new AuthorizationProvider();

                // Note: see AuthorizationController.cs for more
                // information concerning ApplicationCanDisplayErrors.
                options.ApplicationCanDisplayErrors = true;
                options.AllowInsecureHttp = true;

                // Note: by default, tokens are signed using dynamically-generated
                // RSA keys but you can also use your own certificate:
                // options.SigningCredentials.AddCertificate(certificate);
            });

            app.UseStaticFiles();

            app.UseMvc();

            app.UseWelcomePage();

            using (var database = app.ApplicationServices.GetService<ApplicationContext>())
            {
                database.Applications.Add(new Application
                {
                    ApplicationID = "myClient",
                    DisplayName = "My client application",
                    RedirectUri = "http://localhost:59872/signin-oidc",
                    LogoutRedirectUri = "http://localhost:59872/",
                    Secret = "secret_secret_secret"
                });

                database.SaveChanges();
            }
        }
    }
}
