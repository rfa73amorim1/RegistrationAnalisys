using RegistrationAnalisys.Domain.Interfaces;
using RegistrationAnalisys.Domain.Models;
using RegistrationAnalisys.Infrastructure.Services;

namespace RegistrationAnalisys.Infrastructure.Mocks;

public sealed class SerasaMockSource : ISerasaSource
{
    private readonly JsonFileReader _reader;

    public SerasaMockSource(IWebHostEnvironment environment)
    {
        _reader = new JsonFileReader(environment.ContentRootPath);
    }

    public Task<SerasaData> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default)
    {
        var file = SelecionarArquivo(cnpj);
        return _reader.ReadAsync<SerasaData>(Path.Combine("Mocks", "Serasa", file), cancellationToken);
    }

    private static string SelecionarArquivo(string cnpj)
    {
        var ultimoDigito = cnpj[^1];

        return ultimoDigito switch
        {
            '1' => "serasa-ok.json",
            '2' => "serasa-ressalva.json",
            '3' => "serasa-reprovado.json",
            _ => "serasa-ok.json"
        };
    }
}
