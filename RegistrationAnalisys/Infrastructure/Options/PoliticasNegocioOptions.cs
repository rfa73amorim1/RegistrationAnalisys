namespace RegistrationAnalisys.Infrastructure.Options;

public sealed class PoliticasNegocioOptions
{
    public List<PoliticaNegocioOption> Itens { get; set; } = new();
}

public sealed class PoliticaNegocioOption
{
    public string Id { get; set; } = string.Empty;
    public string TipoPapel { get; set; } = "CLIENTE";
    public decimal ScoreMinAprovar { get; set; }
    public decimal ScoreMinRessalva { get; set; }
    public int? MaxDiasAtrasoInterno { get; set; }
    public decimal? LimiteBase { get; set; }
    public int? PrazoMaximoAprovadoDias { get; set; }
    public int? PrazoMaximoRessalvaDias { get; set; }
    public int? EntradaMinimaRessalvaPercentual { get; set; }
    public bool BloqueiaComRestricaoAtiva { get; set; }
}
