# RegistrationAnalisys - One Pager de Pitch

## Problema

Times comerciais B2B precisam decidir rapido se podem vender a prazo para um CNPJ, mas normalmente recebem dados tecnicos dispersos e sem contexto do negocio.

## Solucao

RegistrationAnalisys transforma dados de risco em decisao comercial objetiva para operacao B2B:

1. Recebe CNPJ + dados do pedido + contexto do cliente.
2. Aplica politica de negocio do cliente comprador (por `politicaId`).
3. Retorna resposta enxuta para acao imediata:
   - `recomendacaoAgente`
   - `acaoComercial`
   - `motivosPrincipais`

## Diferencial

Nao entregamos apenas score. Entregamos recomendacao acionavel de venda a prazo, alinhada ao perfil de risco do negocio comprador.

Mesmo CNPJ pode gerar decisoes diferentes conforme a politica aplicada (ex.: alimentos conservador vs bens de consumo padrao).

## Contrato da API (resumo)

Entrada (request unica):

- `cnpj`
- `valorPedido`
- `prazoDesejadoDias`
- `clienteNovo`
- `diasAtrasoInterno90d` (obrigatorio para cliente existente)
- `politicaId`

Saida (response enxuta):

- `recomendacaoAgente`
- `acaoComercial`:
  - `decisao`
  - `limiteCreditoSugerido`
  - `prazoMaximoDias`
  - `entradaMinimaPercentual`
  - `vendaSomenteAVista`
- `motivosPrincipais`

## Como a decisao e feita

1. Consulta fontes mockadas (PoC): Serasa e Certidoes.
2. Aplica regras da politica configurada no `appsettings.json`.
3. Calcula uma das decisoes: APROVADO, APROVADO_COM_RESSALVAS, REPROVADO.

## Prova de valor para negocio

1. Reduz inadimplencia com reguas de risco configuraveis.
2. Mantem conversao comercial com aprovacao condicionada (ressalvas).
3. Acelera decisao com resposta clara para operacao.
4. Escala para segmentos diferentes sem alterar codigo (apenas configuracao de politica).

## Escopo atual da PoC

1. Dados simulados por cenario de CNPJ.
2. Politicas configuradas por arquivo (`PoliticasNegocio:Itens`).
3. Collection pronta para demo com 4 cenarios:
   - APROVADO
   - APROVADO_COM_RESSALVAS
   - REPROVADO
   - erro de validacao

## Proximos passos recomendados

1. Mover politicas para cadastro administravel (DB + tela).
2. Versionar politicas e auditar decisao por cliente.
3. Integrar fontes reais de dados de risco.
4. Medir KPIs: inadimplencia, conversao, tempo medio de decisao.
