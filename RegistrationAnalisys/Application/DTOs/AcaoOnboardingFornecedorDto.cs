namespace RegistrationAnalisys.Application.DTOs;

public sealed class AcaoOnboardingFornecedorDto
{
    public bool HabilitarCadastro { get; set; }
    public string AcaoRecomendada { get; set; } = string.Empty;
    public List<string> Pendencias { get; set; } = new();
    public string NivelRisco { get; set; } = string.Empty;
}
