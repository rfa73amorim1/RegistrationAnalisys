namespace RegistrationAnalysis.Domain.Models;

public sealed class CertidoesData
{
    public string Federal { get; set; } = string.Empty;
    public string Estadual { get; set; } = string.Empty;
    public string Trabalhista { get; set; } = string.Empty;
    public string Fgts { get; set; } = string.Empty;

    public IEnumerable<KeyValuePair<string, string>> ToPairs()
    {
        yield return new KeyValuePair<string, string>("federal", Federal);
        yield return new KeyValuePair<string, string>("estadual", Estadual);
        yield return new KeyValuePair<string, string>("trabalhista", Trabalhista);
        yield return new KeyValuePair<string, string>("fgts", Fgts);
    }
}
