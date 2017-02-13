using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Com.SSO.AuthenticationServer.Services;
using System.Security.Cryptography;
using System;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.EntityFramework.DbContexts;
using System.Linq;
using System.Reflection;

namespace Com.SSO.AuthenticationServer
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
         
            //如果是开发环境
            if (env.IsDevelopment())
            {
                //有关使用用户密钥存储的详细信息，参见
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
               // builder.AddUserSecrets();
            }         

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }



        public IConfigurationRoot Configuration { get; }
        //这个方法被运行时调用。使用此方法将服务添加到容器中。
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //煞笔案例居然不写明没有这个就不能跳转 特么的也是醉了。
            #region MyRegion
            //RSA：证书长度2048以上，否则抛异常
            //配置AccessToken的加密证书
            var rsa = new RSACryptoServiceProvider();
            //从配置文件获取加密证书
            rsa.ImportCspBlob(Convert.FromBase64String(Configuration["SigningCredential"]));
            #endregion


            // Add framework services. 添加框架服务
            //https://docs.efproject.net/en/latest/providers/index.html
            //添加程序集 SapientGuardian.EntityFrameworkCore.MySql
            //services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseMySQL(Configuration.GetConnectionString("DefaultConnection")));

            //services.AddDbContext<ApplicationDbContext>(options =>
            // options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            //services.AddIdentity<ApplicationUser, IdentityRole>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>()
            //    .AddDefaultTokenProviders();

            services.AddMvc();




            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            // configure identity server with in-memory users, but EF stores for clients and resources


            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();


            //IdentityServer4授权服务配置
            services.AddIdentityServer()
                .AddSigningCredential(new RsaSecurityKey(rsa))//设置加密证书
                // .AddTemporarySigningCredential() //测试的时候可使用临时的证书
                .AddInMemoryPersistedGrants()
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryApiResources(Config.GetApiResources())
                .AddInMemoryClients(Config.GetClients())
                .AddInMemoryUsers(Config.GetUsers())
              //  .AddAspNetIdentity<ApplicationUser>()

             .AddConfigurationStore(builder =>
                    builder.UseSqlServer(connectionString, options =>
                        options.MigrationsAssembly(migrationsAssembly)))

                .AddOperationalStore(builder =>
                    builder.UseSqlServer(connectionString, options =>
                        options.MigrationsAssembly(migrationsAssembly)));
        }
        //初始化数据库
        //more see link http://docs.identityserver.io/en/release/quickstarts/8_entity_framework.html?highlight=EntityFramework
        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                context.Database.Migrate();
                if (!context.Clients.Any())
                {
                    foreach (var client in Config.GetClients())
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Config.GetIdentityResources())
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }   
                if (!context.ApiResources.Any())
                {
                    foreach (var resource in Config.GetApiResources())
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }
            }
        }

        //这个方法被运行时调用。用这个方法来配置HTTP请求管道。
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            /// 初始化数据库
            InitializeDatabase(app);
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                //  app.UseExceptionHandler("/Home/Error");
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
    
            app.UseStaticFiles();

          //  app.UseIdentity();
            app.UseIdentityServer();
            //在下面添加外部认证中间件。要配置它们，请参阅
            // Add external authentication middleware below. To configure them please see http://go.microsoft.com/fwlink/?LinkID=532715

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
