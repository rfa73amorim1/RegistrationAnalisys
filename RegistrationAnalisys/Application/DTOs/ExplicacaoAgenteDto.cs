namespace RegistrationAnalisys.Application.DTOs;

public sealed class ExplicacaoAgenteDto
{
    public string Resumo { get; set; } = string.Empty;
    public List<string> Fundamentos { get; set; } = new();
    public List<string> Recomendacoes { get; set; } = new();
}
