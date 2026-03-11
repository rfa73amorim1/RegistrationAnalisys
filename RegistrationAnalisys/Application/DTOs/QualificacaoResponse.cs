namespace RegistrationAnalisys.Application.DTOs;

public sealed class QualificacaoResponse
{
    public string RecomendacaoAgente { get; set; } = string.Empty;
    public AcaoComercialDto AcaoComercial { get; set; } = new();
    public List<string> MotivosPrincipais { get; set; } = new();
}
