namespace RegistrationAnalisys.Application.DTOs;

public sealed class QualificacaoResponse
{
    public string Decisao { get; set; } = string.Empty;
    public string RecomendacaoAgente { get; set; } = string.Empty;
    public AcaoComercialDto? AcaoComercial { get; set; }
    public AcaoOnboardingFornecedorDto? AcaoOnboardingFornecedor { get; set; }
    public List<string> MotivosPrincipais { get; set; } = new();
}
