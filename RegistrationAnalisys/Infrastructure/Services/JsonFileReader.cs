using System.Text.Json;

namespace RegistrationAnalisys.Infrastructure.Services;

public sealed class JsonFileReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _contentRootPath;

    public JsonFileReader(string contentRootPath)
    {
        _contentRootPath = contentRootPath;
    }

    public async Task<T> ReadAsync<T>(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_contentRootPath, relativePath);
        var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
        var result = JsonSerializer.Deserialize<T>(json, JsonOptions);

        if (result is null)
        {
            throw new InvalidOperationException($"Nao foi possivel desserializar o arquivo '{relativePath}'.");
        }

        return result;
    }
}
