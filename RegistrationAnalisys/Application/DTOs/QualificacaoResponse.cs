namespace RegistrationAnalisys.Application.DTOs;

public sealed class QualificacaoResponse
{
    public string Cnpj { get; set; } = string.Empty;
    public string DecisaoFinal { get; set; } = string.Empty;
    public decimal ScoreFinanceiro { get; set; }
    public Dictionary<string, string> ResultadoCnds { get; set; } = new();
    public List<string> Evidencias { get; set; } = new();
    public List<string> Pendencias { get; set; } = new();
    public ExplicacaoAgenteDto? ExplicacaoAgente { get; set; }
}
