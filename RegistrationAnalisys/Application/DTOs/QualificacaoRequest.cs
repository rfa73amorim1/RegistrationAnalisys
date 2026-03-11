namespace RegistrationAnalisys.Application.DTOs;

public sealed class QualificacaoRequest
{
    public string Cnpj { get; set; } = string.Empty;
    public decimal ValorPedido { get; set; }
    public int PrazoDesejadoDias { get; set; }
    public bool ClienteNovo { get; set; }
    public int? DiasAtrasoInterno90d { get; set; }
    public string PoliticaId { get; set; } = string.Empty;
}
