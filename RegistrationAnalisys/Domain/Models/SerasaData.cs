namespace RegistrationAnalisys.Domain.Models;

public sealed class SerasaData
{
    public decimal Score { get; set; }
    public string Faixa { get; set; } = string.Empty;
    public decimal Endividamento { get; set; }
    public int Atrasos { get; set; }
}
