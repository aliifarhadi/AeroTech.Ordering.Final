using System.Reflection;
using System.Text.Json.Serialization;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Framework.Presentation.AspNetCore.Services;
using AeroTech.Framework.Presentation.Filters;
using AeroTech.Framework.Presentation.HealthChecks;
using AeroTech.Framework.Presentation.Json;
using AeroTech.Framework.Presentation.Middlewares;
using AeroTech.Framework.Presentation.Options;
using AeroTech.Framework.Presentation.Swagger;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace AeroTech.Framework.Presentation.Extensions
{
    public static class PresentationExtensions
    {
        public static IServiceCollection AddPresentation(
            this IServiceCollection services,
            IConfiguration configuration,
            params Assembly[] controllerAssemblies)
        {
            services.AddHttpContextAccessor();
            services.TryAddSingleton<IActorResolver, ClaimsActorResolver>();

            var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

            ArgumentException.ThrowIfNullOrWhiteSpace(jwt.Authority, $"{JwtOptions.SectionName}:{nameof(JwtOptions.Authority)}");
            ArgumentException.ThrowIfNullOrWhiteSpace(jwt.Audience, $"{JwtOptions.SectionName}:{nameof(JwtOptions.Audience)}");

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = jwt.Authority;
                    options.Audience = jwt.Audience;
                    options.RequireHttpsMetadata = jwt.RequireHttpsMetadata;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwt.Authority,
                        ValidAudience = jwt.Audience,
                        ValidAlgorithms = [SecurityAlgorithms.RsaSha256, SecurityAlgorithms.EcdsaSha256]
                    };
                });

            var mvc = services
                .AddControllers(options => options.Filters.Add<ApiResultWrapperFilter>())
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.Converters.Add(new LongToStringJsonConverter());
                    options.JsonSerializerOptions.Converters.Add(new NullableLongToStringJsonConverter());
                });

            foreach (var assembly in controllerAssemblies)
                mvc.AddApplicationPart(assembly);

            services
                .AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ReportApiVersions = true;
                })
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter a JWT bearer token."
                });

                options.DocumentFilter<BearerSecurityRequirementDocumentFilter>();
            });
            // "self" is a dependency-free check tagged "live": it succeeds as long as the
            // process can serve HTTP, which is exactly what a Kubernetes liveness probe wants.
            services
                .AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "live" });

            return services;
        }

        public static WebApplication UsePresentation(this WebApplication app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AeroTech Ordering API v1");
                    options.RoutePrefix = string.Empty;
                });
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // Kubernetes liveness probe: only the dependency-free "live" checks.
            // Failure here means the process is wedged and the pod should be restarted.
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live"),
                ResponseWriter = HealthCheckResponseWriter.WriteJsonResponse
            });

            // Kubernetes readiness probe: dependency checks tagged "ready" (SQL Server,
            // Redis, message bus). Failure pulls the pod out of the Service load balancer
            // without restarting it, so it can recover once dependencies come back.
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("ready"),
                ResponseWriter = HealthCheckResponseWriter.WriteJsonResponse
            });

            // Aggregate endpoint (every registered check) — handy for humans/dashboards.
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = HealthCheckResponseWriter.WriteJsonResponse
            });

            return app;
        }
    }
}
