using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace WebApiDistributedSqlCache
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

            bool dcachepgsqlserver = Configuration.GetSection("UseCache").GetValue<bool>(@"PgSql");
            bool dcachesqlserver = Configuration.GetSection("UseCache").GetValue<bool>(@"SqlServer");
            bool dcacheredis = Configuration.GetSection("UseCache").GetValue<bool>(@"Redis");
            bool dcacheInmemory = Configuration.GetSection("UseCache").GetValue<bool>(@"InMemory");
            dcachepgsqlserver = dcachesqlserver = dcacheredis = false;
            dcacheInmemory = true;
            if (dcachepgsqlserver)
            services.AddDistributedPostgreSqlCache(setup =>
            {
                setup.ConnectionString = Configuration.GetConnectionString("PGSqlCacheDbConnection"); ;
                setup.SchemaName = Configuration.GetSection("CacheStorage").GetValue<string>(@"PGSchemaName");
                setup.TableName = Configuration.GetSection("CacheStorage").GetValue<string>(@"PGTableName");
                setup.DisableRemoveExpired = false;// Configuration["DisableRemoveExpired"];
                // Optional - DisableRemoveExpired default is FALSE
                setup.CreateInfrastructure = true;// Configuration["CreateInfrastructure"];
                // CreateInfrastructure is optional, default is TRUE
                // This means que every time starts the application the
                // creation of table and database functions will be verified.
                setup.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);
                // ExpiredItemsDeletionInterval is optional
                // This is the periodic interval to scan and delete expired items in the cache. Default is 30 minutes.
                // Minimum allowed is 5 minutes. - If you need less than this please share your use case 😁, just for curiosity...
                });

            if (!dcachepgsqlserver && !dcachesqlserver && !dcacheredis)
            {
                 services.AddDistributedMemoryCache();
            }
            if (dcachesqlserver)
            {
                services.AddDistributedSqlServerCache(options =>
                {
                    options.ConnectionString = Configuration.GetConnectionString("SQLServerCacheDbConnection");
                    options.SchemaName = Configuration.GetSection("CacheStorage").GetValue<string>(@"SchemaName");// "dbo";
                    options.TableName = Configuration.GetSection("CacheStorage").GetValue<string>(@"TableName");// "CacheStore";
                });
            }
            //
            if (dcacheInmemory)
            {
                services.AddMemoryCache(options =>
                {
                    // Overall 1024 size (no unit)
                    options.SizeLimit = 1024;
                });
            }
            if (dcacheredis)
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = Configuration.GetConnectionString("RedisConnection");//Configuration["RedisConStr"];
                    options.InstanceName = "SampleInstance";
                });
            }

            //
            // services.AddDistributedRedisCache(options => {
            //     options.Configuration = "localhost:6379";
            //     options.InstanceName = "";
            // });
            //
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApiDistributedSqlCache", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApiDistributedSqlCache v1"));
            }

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
