using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace CNPJ.Processing.Api.Config
{
    public static class HealthCheckConfig
    {
        public static IServiceCollection AddHealthCheck(this IServiceCollection services)
        {
            services.AddHealthChecks();
            services.AddHealthChecksUI(opt =>
            {
                opt.SetEvaluationTimeInSeconds(15);
                opt.MaximumHistoryEntriesPerEndpoint(60);
                opt.SetApiMaxActiveRequests(1);
                opt.AddHealthCheckEndpoint("cnpjprocessing", "/cnpjprocessing/hc");
            })
            .AddInMemoryStorage();
            return services;
        }

        public static IApplicationBuilder UseHealthCheckConfig(this IApplicationBuilder app)
        {
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();

            app.UseHealthChecksWithLogging("/cnpjprocessing/hc", loggerFactory);

            app.UseHealthChecksUI();
            return app;
        }

        private static IApplicationBuilder UseHealthChecksWithLogging(
            this IApplicationBuilder app,
            string path,
            ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("HealthChecks");

            return app.UseHealthChecks(path, new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = async (context, report) =>
                {
                    var details = report.Entries.Select(e => new
                    {
                        Component = e.Key,
                        Status = e.Value.Status.ToString(),
                        Description = e.Value.Description,
                        Duration = e.Value.Duration.TotalMilliseconds
                    }).ToList();

                    logger.LogInformation(
                        "Health check executado. Status: {Status}, Cliente: {ClientIP}, Duração: {Duration}ms, Detalhes: {@Details}",
                        report.Status.ToString(),
                        context.Connection.RemoteIpAddress,
                        report.TotalDuration.TotalMilliseconds,
                        details);

                    await UIResponseWriter.WriteHealthCheckUIResponse(context, report);
                }
            });
        }
    }
}