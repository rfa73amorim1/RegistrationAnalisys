namespace RegistrationAnalysis.Domain.Models;

public sealed class ConsolidadoData
{
    public SerasaData Serasa { get; set; } = new();
    public CertidoesData Certidoes { get; set; } = new();
}
