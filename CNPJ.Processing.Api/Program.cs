using CNPJ.Processing.Api.Config;
using CNPJ.Processing.Core.Validators;
using CNPJ.Processing.CrossCutting.IoC;
using CNPJ.Processing.Infra.Kafka;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogConfiguration(builder.Configuration);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);


builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
    });

builder.Services.AddMvc(opt =>
{
    opt.Filters.Add(typeof(ValidatorActionFilter));
}).AddNewtonsoftJson();

builder.Services.AddCors(options => 
    options.AddPolicy("AllowAll", p => 
        p.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddSwaggerConfig();

builder.Services.AddHealthCheck();

builder.Services.AddSetupIoC(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLoggingCustom();

if (builder.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseHsts();

app.UseRequestLocalization("pt-BR");

app.UseSwaggerConfig();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseHealthCheckConfig();

using (var scope = app.Services.CreateScope())
{
    var kafkaHelper = scope.ServiceProvider.GetRequiredService<IKafkaHelper>();
    KafkaHelper.Configure(kafkaHelper);
}

return app.RunWithSerilog();