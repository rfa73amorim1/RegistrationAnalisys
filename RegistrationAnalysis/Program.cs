using RegistrationAnalysis.Application.Services;
using RegistrationAnalysis.Domain.Interfaces;
using RegistrationAnalysis.Infrastructure.Mocks;
using RegistrationAnalysis.Infrastructure.Options;
using RegistrationAnalysis.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection("AzureOpenAI"));
builder.Services.Configure<PoliticasNegocioOptions>(builder.Configuration.GetSection("PoliticasNegocio"));
builder.Services.AddHttpClient("AzureOpenAI", client =>
{
	client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddScoped<ISerasaSource, SerasaMockSource>();
builder.Services.AddScoped<ICertidoesSource, CertidoesMockSource>();
builder.Services.AddScoped<ExplicadorQualificacaoTemplate>();
builder.Services.AddScoped<IExplicadorQualificacao, ExplicadorQualificacaoAzure>();
builder.Services.AddScoped<IQualificacaoService, QualificacaoService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
