# RegistrationAnalisys MVP

API Web em .NET 6 para qualificar CNPJ com base em fontes mockadas (Serasa e Certidoes).

## Como rodar

```bash
dotnet restore
dotnet run --project RegistrationAnalisys/RegistrationAnalisys.csproj
```

Acesse o Swagger em `https://localhost:xxxx/swagger` (porta exibida no terminal).

## Endpoint

- `POST /qualificacoes?includeExplanation=true`

Request:

```json
{
  "cnpj": "12.345.678/0001-91"
}
```

Response (exemplo):

```json
{
  "cnpj": "12345678000191",
  "decisaoFinal": "APROVADO",
  "scoreFinanceiro": 8.7,
  "evidencias": [
    "Score Serasa: 8.7 (faixa A).",
    "Endividamento informado: 12.4%.",
    "Quantidade de atrasos: 0."
  ],
  "pendencias": [],
  "explicacaoAgente": "A decisao final foi APROVADO com score financeiro 8.7. Evidencias principais: Score Serasa: 8.7 (faixa A).; Endividamento informado: 12.4%.. A avaliacao considera apenas os dados tecnicos retornados pelas fontes mockadas."
}
```

## Variacao de cenarios por CNPJ

- Final `1`: cenario OK
- Final `2`: cenario de RESSALVAS (Serasa com score intermediario)
- Final `3`: cenario REPROVADO (certidao pendente)
