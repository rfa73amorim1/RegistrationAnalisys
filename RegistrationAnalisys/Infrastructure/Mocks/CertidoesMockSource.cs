using RegistrationAnalisys.Domain.Interfaces;
using RegistrationAnalisys.Domain.Models;
using RegistrationAnalisys.Infrastructure.Services;

namespace RegistrationAnalisys.Infrastructure.Mocks;

public sealed class CertidoesMockSource : ICertidoesSource
{
    private readonly JsonFileReader _reader;

    public CertidoesMockSource(IWebHostEnvironment environment)
    {
        _reader = new JsonFileReader(environment.ContentRootPath);
    }

    public Task<CertidoesData> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default)
    {
        var file = SelecionarArquivo(cnpj);
        return _reader.ReadAsync<CertidoesData>(Path.Combine("Mocks", "Certidoes", file), cancellationToken);
    }

    private static string SelecionarArquivo(string cnpj)
    {
        var ultimoDigito = cnpj[^1];

        return ultimoDigito switch
        {
            '1' => "certidoes-ok.json",
            '2' => "certidoes-ok.json",
            '3' => "certidoes-positiva.json",
            _ => "certidoes-ok.json"
        };
    }
}
