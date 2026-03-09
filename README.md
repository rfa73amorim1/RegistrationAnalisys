# RegistrationAnalisys MVP

API Web em .NET 6 para qualificar CNPJ com base em fontes mockadas (Serasa e Certidoes).

Importante: este projeto usa dados simulados para testes de hackathon. Nao ha consulta real a bureaus externos neste MVP.

## Como rodar

```bash
dotnet restore
dotnet run --project RegistrationAnalisys/RegistrationAnalisys.csproj
```

URLs locais (conforme `launchSettings.json`):

- `https://localhost:7202`
- `http://localhost:5168`

Swagger foi desabilitado para uso direto com Postman.

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
  "scoreFinanceiro": 870,
  "resultadoCnds": {
    "federal": "NEGATIVA_SEM_RESTRICAO",
    "estadual": "NEGATIVA_SEM_RESTRICAO",
    "trabalhista": "NEGATIVA_SEM_RESTRICAO",
    "fgts": "NEGATIVA_SEM_RESTRICAO"
  },
  "evidencias": [
    "Score Serasa: 870 (faixa A).",
    "Endividamento informado: 12.4%.",
    "Quantidade de atrasos: 0."
  ],
  "pendencias": [],
  "explicacaoAgente": {
    "resumo": "A decisao final foi APROVADO com score financeiro 870.",
    "fundamentos": [
      "Evidencias principais: Score Serasa: 870 (faixa A).; Endividamento informado: 12.4%."
    ],
    "recomendacoes": []
  }
}
```

## Cenarios de teste (simulados)

Para facilitar testes no Postman, o ultimo digito do CNPJ define o cenario mockado:

- Final `1`: cenario OK
- Final `2`: cenario APROVADO_COM_RESSALVAS
- Final `3`: cenario REPROVADO (CND positiva com restricao)

Esses cenarios sao artificiais e servem apenas para validar o fluxo de decisao.

### Requests sugeridos para teste

1. Aprovado

```http
POST https://localhost:7202/qualificacoes?includeExplanation=true
Content-Type: application/json

{"cnpj":"12.345.678/0001-91"}
```

2. Aprovado com ressalvas

```http
POST https://localhost:7202/qualificacoes?includeExplanation=true
Content-Type: application/json

{"cnpj":"12.345.678/0001-92"}
```

3. Reprovado

```http
POST https://localhost:7202/qualificacoes?includeExplanation=true
Content-Type: application/json

{"cnpj":"12.345.678/0001-93"}
```

4. Validacao de entrada (erro 400)

```http
POST https://localhost:7202/qualificacoes
Content-Type: application/json

{"cnpj":"123"}
```

## Regras de decisao (MVP)

1. Se qualquer CND estiver `POSITIVA` (com restricao) => `REPROVADO`
2. Senao, se `score >= 800` => `APROVADO`
3. Senao, se `score >= 600` => `APROVADO_COM_RESSALVAS`
4. Senao (`score < 600`) => `REPROVADO`

## Modelo de CND no MVP

- `NEGATIVA`: sem restricao
- `POSITIVA`: com restricao

