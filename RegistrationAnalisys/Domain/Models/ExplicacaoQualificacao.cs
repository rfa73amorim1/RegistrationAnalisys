namespace RegistrationAnalisys.Domain.Models;

public sealed class ExplicacaoQualificacao
{
    public string Resumo { get; set; } = string.Empty;
    public List<string> Fundamentos { get; set; } = new();
    public List<string> Recomendacoes { get; set; } = new();
    public string Origem { get; set; } = "REGRA";
}
