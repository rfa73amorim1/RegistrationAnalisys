namespace RegistrationAnalysis.Application.DTOs;

public sealed class QualificacaoRequest
{
    public string Cnpj { get; set; } = string.Empty;
    public string Papel { get; set; } = "CLIENTE";
    public decimal ValorOperacao { get; set; }
    public int PrazoOprecaoDias { get; set; }
    public bool RelacionamentoNovo { get; set; }
    public int? DiasAtrasoInterno90d { get; set; }
    public string PoliticaId { get; set; } = string.Empty;
}
