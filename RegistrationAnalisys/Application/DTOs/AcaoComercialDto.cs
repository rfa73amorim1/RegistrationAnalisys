namespace RegistrationAnalisys.Application.DTOs;

public sealed class AcaoComercialDto
{
    public string Decisao { get; set; } = string.Empty;
    public decimal LimiteCreditoSugerido { get; set; }
    public int PrazoMaximoDias { get; set; }
    public int EntradaMinimaPercentual { get; set; }
    public bool VendaSomenteAVista { get; set; }
}
