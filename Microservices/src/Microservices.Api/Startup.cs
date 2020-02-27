using Microservices.Api.Handlers;
using Microservices.Api.Repositories;
using Microservices.Common.Auth;
using Microservices.Common.Events;
using Microservices.Common.Mongo;
using Microservices.Common.RabbitMq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace Microservices.Api
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
            services.AddMvc();
            services.AddJwt(Configuration);
            services.AddMongoDB(Configuration);
            services.AddRabbitMq(Configuration);
            services.AddScoped<IEventHandler<ActivityCreated>, ActivityCreatedHandler>();
            services.AddScoped<IActivityRepository, ActivityRepository>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Microservices API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var serviceScopeFactory = app.ApplicationServices.GetService<IServiceScopeFactory>();
            using (var scope = serviceScopeFactory.CreateScope())
            {
                scope.ServiceProvider
                    .GetService<IDatabaseInitializer>().InitializeAsync();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Microservices API V1");
            });

            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
