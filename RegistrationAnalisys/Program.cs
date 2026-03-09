using RegistrationAnalisys.Application.Services;
using RegistrationAnalisys.Domain.Interfaces;
using RegistrationAnalisys.Infrastructure.Mocks;
using RegistrationAnalisys.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddScoped<ISerasaSource, SerasaMockSource>();
builder.Services.AddScoped<ICertidoesSource, CertidoesMockSource>();
builder.Services.AddScoped<IExplicadorQualificacao, ExplicadorQualificacaoTemplate>();
builder.Services.AddScoped<IQualificacaoService, QualificacaoService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
