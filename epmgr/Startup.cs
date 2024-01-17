using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using epmgr.Data;
using epmgr.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Hosting;
using epmgr.Guru;
using epmgr.Hubs;
using System;
using epmgr.Data.ywsd;
using epmgr.Filter;
using epmgr.Gsm;
using epmgr.Omm;
using epmgr.Ywsd;
using StackExchange.Redis;
using Hangfire;
using Hangfire.PostgreSql;
using Npgsql;

namespace epmgr
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
            services.Configure<Settings>(Configuration.GetSection("Settings"));
            services.Configure<OmmSettings>(Configuration.GetSection("Omm"));
            services.Configure<GuruSettings>(Configuration.GetSection("Guru"));
            services.Configure<GsmSettings>(Configuration.GetSection("Gsm"));
            services.Configure<YwsdSettings>(Configuration.GetSection("Ywsd"));
            services.Configure<MapSettings>(Configuration.GetSection("Map"));

            var defaultDatasourceBuilder = new NpgsqlDataSourceBuilder(Configuration.GetConnectionString("DefaultConnection"));
            defaultDatasourceBuilder.MapEnum<MgrExtensionType>();
            var defaultDatasource = defaultDatasourceBuilder.Build();
            services.AddDbContext<MgrDbContext>(x => x.UseNpgsql(defaultDatasource));

            var ywsdDatasourceBuilder = new NpgsqlDataSourceBuilder(Configuration.GetConnectionString("Ywsd"));
            ywsdDatasourceBuilder
                .MapEnum<ExtensionType>("extension_type")
                .MapEnum<ForwardingMode>("forwarding_mode")
                .MapEnum<ForkRankMode>("fork_rank_mode")
                .MapEnum<ForkRankMemberType>("fork_rankmember_type");
            var ywsdDatasource = ywsdDatasourceBuilder.Build();
            services.AddDbContext<YwsdDbContext>(x => x.UseNpgsql(ywsdDatasource));

            services.RegisterYateDb<DectDbContext, YateDectSyncService>(Configuration.GetConnectionString("YateDect"));
            services.RegisterYateDb<SipDbContext, YateSipSyncService>(Configuration.GetConnectionString("YateSip"));
            services.RegisterYateDb<AppDbContext, YateAppSyncService>(Configuration.GetConnectionString("YateApp"));
            services.RegisterYateDb<PremiumDbContext, YatePremiumSyncService>(Configuration.GetConnectionString("YatePremium"));
            services.RegisterYateDb<GsmDbContext, YateGsmSyncService>(Configuration.GetConnectionString("YateGsm"));
            services.AddSignalR();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(x=>
                {
                    x.Cookie.Name = ".auth";
                    x.LoginPath = "/admin/login";
                });
            services.AddRouting(options => options.LowercaseUrls = true);
            
            services.AddControllers();
            services.AddRazorPages(options =>
                {
                    options.Conventions.AuthorizeFolder("/");
                    options.Conventions.AllowAnonymousToPage("/Index");
                    options.Conventions.AllowAnonymousToPage("/Admin/Login");
                    options.Conventions.AllowAnonymousToPage("/Error");
                });
            services.AddCors();
            var settings = Configuration.GetSection("Omm").Get<OmmSettings>();
            services.RegisterOmm(settings);
            services.AddSingleton<MapHub>();
            services.AddTransient<GuruClient>();
            services.AddSingleton<GuruWebsocket>();
            services.AddSingleton<StatsService>();
            services.AddHostedService<StatsService>();
            services.AddHostedService<GuruSyncService>();
            var redisConnection = Configuration.GetConnectionString("Redis");
            if (!String.IsNullOrEmpty(redisConnection))
            {
                services.AddSingleton(x => ConnectionMultiplexer.Connect(redisConnection));
                services.AddSingleton<YateStatusService>();
            }
#if !DEBUG
            services.AddHangfire(x => x
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(y => y.UseNpgsqlConnection(Configuration.GetConnectionString("DefaultConnection"))));
            services.AddHangfireServer();
#endif
            services.AddTransient<RandomPasswordService>();
            services.AddTransient<YateService>();
            services.AddScoped<YwsdClient>();

            RegisterGuruServices(services);
            RegisterExtensionServices(services);

            services.RegisterGsm(Configuration.GetSection("Gsm").Get<GsmSettings>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, MgrDbContext context)
        {
            context.Database.Migrate();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error/500");
            }
            app.UseStatusCodePagesWithReExecute("/Error/{0}");

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = new FileExtensionContentTypeProvider {Mappings = {[".jnlp"] = "application/x-java-jnlp-file"}}
            });

            app.UseRouting();
            app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(c =>
            {
                c.MapHub<MapHub>("/map");
                c.MapHub<StatusHub>("/ystatus");
                c.MapControllerRoute(name:"api",pattern:"api/{controller}/{action=Index}/{id?}");
                c.MapRazorPages();
            });
#if !DEBUG
            app.UseHangfireDashboard("/jobs", new DashboardOptions {Authorization = new[] {new HangfireAuthorizationFilter()}});
#endif
        }

        private void RegisterGuruServices(IServiceCollection services)
        {
            services.AddScoped<IGuruMessageHandler, MgrSyncService>();
            services.AddScoped<IGuruMessageHandler, YwsdSyncService>();
        }

        private void RegisterExtensionServices(IServiceCollection services)
        {
            services.AddScoped<IExtensionService, YwsdSyncService>();
        }
    }

    public static class RegisterExtensions
    {
        public static void RegisterYateDb<TDbContext, TYateService>(this IServiceCollection services, string connectionString) where TDbContext : DbContext where TYateService: class, IYateService,IGuruMessageHandler,IExtensionService
        {
            if (String.IsNullOrEmpty(connectionString)) return;
            var datasourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            datasourceBuilder.MapEnum<DectDisplayModus>("dect_displaymode");
            var dataSource = datasourceBuilder.Build();
            services.AddDbContext<TDbContext>(x => x.UseNpgsql(dataSource));
            services.AddScoped<IYateService, TYateService>();
            services.AddScoped<TYateService>();
            services.AddScoped<IGuruMessageHandler, TYateService>();
            services.AddScoped<IExtensionService, TYateService>();
        }

        public static void RegisterGsm(this IServiceCollection services, GsmSettings settings)
        {
            if (settings is null || String.IsNullOrEmpty(settings.Endpoint)) return;
            services.AddSingleton<IGsmClient, GsmClient>();
            services.AddScoped<IExtensionService, GsmSyncService>();
            services.AddScoped<IGuruMessageHandler, GsmSyncService>();
        }

        public static void RegisterOmm(this IServiceCollection services, OmmSettings settings)
        {
            if (settings is null || String.IsNullOrEmpty(settings.Hostname)) return;
            services.AddSingleton<IOmmClient>(new MitelClient(settings.Hostname, settings.Port));
            services.AddSingleton<OmmEventService>();
            services.AddScoped<IGuruMessageHandler, OmmSyncService>();
            services.AddScoped<OmmSyncService>();
            services.AddScoped<IExtensionService, OmmSyncService>();
            services.AddHostedService<OmmEventService>();
            services.AddHostedService<RfpEventService>();
        }
    }
}
