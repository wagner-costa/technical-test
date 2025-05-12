using Serilog;
using Serilog.Events;

namespace CNPJ.Processing.Api.Config
{
    public static class SerilogConfig
    {
        public static IHostBuilder AddSerilogConfiguration(this IHostBuilder builder, IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .CreateLogger();

            builder.UseSerilog();
            builder.AddSerilogWithHealthCheckLogging(configuration);

            return builder;
        }

        public static IApplicationBuilder UseSerilogRequestLoggingCustom(this IApplicationBuilder app)
        {
            return app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"]);
                    diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);

                    if (httpContext.User.Identity?.IsAuthenticated == true)
                    {
                        diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
                    }
                };
            });
        }

        public static int RunWithSerilog(this WebApplication app)
        {
            try
            {
                Log.Information("Iniciando aplicação");
                app.Run();
                Log.Information("Aplicação encerrada com sucesso");
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "A aplicação terminou inesperadamente");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder AddSerilogWithHealthCheckLogging(
           this IHostBuilder builder,
           IConfiguration configuration)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("IsHealthCheck", true);

            loggerConfiguration
                .MinimumLevel.Override("HealthChecks", LogEventLevel.Information);

            Log.Logger = loggerConfiguration.CreateLogger();

            builder.UseSerilog();

            return builder;
        }
    }
}