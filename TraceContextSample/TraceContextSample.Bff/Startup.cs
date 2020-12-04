using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using TraceContextSample.Net;
using TraceContextSample.Net.Clients;

namespace TraceContextSample.Bff
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient<ITokenClient, TokenClient>();
            services.AddHttpClient<IBackend1Client, Backend1Client>(configure =>
            {
                configure.BaseAddress = new Uri(Constants.Backend1.BaseUri);
            });
            services.AddHttpClient<IBackend2Client, Backend2Client>(configure =>
            {
                configure.BaseAddress = new Uri(Constants.Backend2.BaseUri);
            });

            services.AddControllers();

            // accepts any access token issued by identity server
            services.AddAuthentication( "Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = Constants.Authority.BaseUri;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiScope", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", Constants.Bff.ResourceName);
                });
            });

            services.AddCors(options =>
                options.AddPolicy("AllowAnyOrigin", 
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                    ));

            services.AddSwaggerDocument();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("AllowAnyOrigin");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
