Vou seguir o escopo de Product Owner definido no prompt referenciado e comparar os requisitos com o que já existe na solução antes de consolidar a visão e as decisões.

Read
Completed with input: {
"path": "C:\\Projetos\\bytepizza\\BytePizza\\.github\\prompts\\00-po.prompt.md"
}

Read
Completed with input: {
"path": "C:\\Projetos\\bytepizza\\BytePizza\\BytePizza.slnx"
}

Ask User
Completed with input: {
"message": "Antes de fechar a especificação e as decisões arquiteturais, preciso fixar alguns pontos que mudam o modelo de domínio e o escopo do MVP. Se preferir, aceite as opções recomendadas como premissas provisórias.",
"requestedSchema": {
"properties": {
"branch_scope": {
"type": "string",
"title": "Unidades/filiais",
"description": "Escopo inicial do sistema",
"oneOf": [
{
"const": "single",
"title": "Uma pizzaria/filial (recomendado para MVP)"
},
{
"const": "multi",
"title": "Múltiplas filiais desde o início"
},
{
"const": "undecided",
"title": "Ainda não decidido"
}
],
"default": "single"
},
"app_audience": {
"type": "string",
"title": "Público do aplicativo",
"description": "Quem usará o cliente MAUI",
"oneOf": [
{
"const": "staff",
"title": "Somente equipe (atendimento, cozinha, entregadores)"
},
{
"const": "customer",
"title": "Somente clientes finais"
},
{
"const": "both",
"title": "Equipe e clientes finais"
},
{
"const": "undecided",
"title": "Ainda não decidido"
}
],
"default": "staff"
},
"payment_scope": {
"type": "string",
"title": "Pagamentos",
"description": "Integração financeira pretendida no MVP",
"oneOf": [
{
"const": "manual",
"title": "Registro manual (dinheiro/cartão/Pix fora do sistema)"
},
{
"const": "gateway",
"title": "Integração com gateway/adquirente já no MVP"
},
{
"const": "defer",
"title": "Pagamentos fora do escopo inicial (recomendado)"
},
{
"const": "undecided",
"title": "Ainda não decidido"
}
],
"default": "defer"
},
"inventory_policy": {
"type": "string",
"title": "Controle de estoque",
"description": "Como controlar consumo de insumos",
"oneOf": [
{
"const": "recipes",
"title": "Receitas/fichas técnicas e movimentação por pedido (recomendado)"
},
{
"const": "manual",
"title": "Ajustes manuais de saldo, sem baixa por receita"
},
{
"const": "later",
"title": "Estoque fora do MVP"
}
],
"default": "recipes"
},
"flavor_pricing": {
"type": "string",
"title": "Preço de pizza com vários sabores",
"description": "Regra para calcular sabores combinados",
"oneOf": [
{
"const": "highest",
"title": "Maior preço entre os sabores (recomendado como padrão inicial)"
},
{
"const": "weighted",
"title": "Preço proporcional às frações de cada sabor"
},
{
"const": "configured",
"title": "Regra configurável por produto/tamanho"
},
{
"const": "undecided",
"title": "Ainda não decidido"
}
],
"default": "configured"
},
"database_choice": {
"type": "string",
"title": "Banco de dados",
"description": "Escolha do provedor suportado em produção",
"oneOf": [
{
"const": "postgres",
"title": "PostgreSQL"
},
{
"const": "sqlserver",
"title": "SQL Server"
},
{
"const": "undecided",
"title": "Ainda não decidido (manter a decisão pendente)"
}
],
"default": "undecided"
},
"offline_policy": {
"type": "string",
"title": "Operação sem conexão",
"description": "O que o aplicativo deve permitir offline",
"oneOf": [
{
"const": "online",
"title": "Online obrigatório; não enviar pedidos offline (recomendado)"
},
{
"const": "draft",
"title": "Permitir rascunhos locais, sincronizados ao reconectar"
},
{
"const": "orders",
"title": "Permitir criar pedidos completos offline e sincronizar depois"
},
{
"const": "undecided",
"title": "Ainda não decidido"
}
],
"default": "online"
}
}
}
}

# Visão e análise de requisitos — BytePizza

Segui o escopo de Product Owner descrito em `00-po.prompt.md` e considerei as escolhas que você informou: **múltiplas filiais**, aplicativo para **equipe e clientes**, pagamentos integrados **fora do MVP**, estoque **por receitas**, preço de sabores **proporcional às frações**, **SQL Server** e operação **online obrigatória**.

> **Limite desta análise:** a solução atual contém apenas `<Solution />` em `BytePizza.slnx`. Portanto, isto analisa a especificação fornecida, não uma implementação existente.

## 1. Visão do produto

O BytePizza deve centralizar a operação de uma ou mais filiais, conectando catálogo, pedidos, cozinha, estoque, clientes e entregas. O objetivo principal é permitir que cada filial processe pedidos corretamente e acompanhe sua execução em tempo real, mantendo preços, estoque, permissões e histórico rastreáveis.

### Perfis de usuário

- **Administrador:** configura filiais, catálogo, preços, usuários e permissões.
- **Atendente:** registra e acompanha pedidos da filial autorizada.
- **Cozinha:** visualiza pedidos confirmados e atualiza seu andamento.
- **Entregador:** consulta entregas atribuídas e atualiza sua conclusão.
- **Cliente:** consulta o catálogo e faz pedidos no aplicativo destinado a clientes.

**Recomendação de experiência:** oferecer interfaces distintas para cliente e equipe, mesmo que compartilhem API e componentes técnicos. O KDS deve ser uma experiência operacional própria para Windows, sem expor telas administrativas ou dados de outros perfis.

## 2. Decisões arquiteturais recomendadas

| ID     | Decisão recomendada                                                                                                                   | Motivo e consequência                                                                                                                                                                                      |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ADR-01 | **Monólito modular** inicialmente, dividido por domínios; não iniciar com microserviços.                                              | Mantém deploy e transações locais simples. Permite separar serviços depois, quando escala ou autonomia operacional justificar o custo.                                                                     |
| ADR-02 | Organizar o backend por **contextos de negócio**: Catálogo, Pedidos, Clientes, Estoque, Atendimento/Entrega, Identidade e Pagamentos. | Evita que “Domain/Application/Infrastructure” se tornem camadas únicas e acopladas, mantendo responsabilidade e propriedade dos dados explícitas.                                                          |
| ADR-03 | Adotar **SQL Server**, como escolhido, com uma única base no início e propriedade das tabelas definida por contexto.                  | Evita manter dois provedores sem necessidade e permite transações locais entre pedido e reserva de estoque. Não permitir que módulos consultem diretamente tabelas alheias.                                |
| ADR-04 | Representar cada filial por `BranchId` nos dados operacionais. Separar **filial** de **tenant SaaS**.                                 | A escolha por múltiplas filiais não implica, por si só, vender o sistema para organizações independentes. Acesso a pedidos, estoque, usuários e relatórios deve ser sempre filtrado por filial autorizada. |
| ADR-05 | Manter catálogo central com configuração de **preço e disponibilidade por filial**.                                                   | Permite administração compartilhada sem impedir que uma filial altere disponibilidade ou preço local. Essa regra deve ser confirmada como política comercial.                                              |
| ADR-06 | Usar consultas de leitura com projeção direta para DTO e `AsNoTracking`; usar casos de uso explícitos para comandos.                  | Atende à otimização solicitada sem exigir CQRS distribuído ou um barramento de mensagens no MVP.                                                                                                           |
| ADR-07 | Calcular preços no servidor e gravar **snapshot** dos itens, opções, preços e totais no pedido.                                       | Preserva o valor vendido mesmo que o catálogo ou o preço mude depois. O cliente não deve ser fonte confiável de valores.                                                                                   |
| ADR-08 | Separar estado do pedido, estado do pagamento e estado da entrega.                                                                    | Evita misturar, por exemplo, “pago” com “em preparo” ou “saiu para entrega”.                                                                                                                               |
| ADR-09 | Propagar eventos de atualização do pedido ao KDS com **SignalR**, mantendo a API como fonte autoritativa.                             | Atualizações em tempo real devem ocorrer após persistência; reconexões devem permitir buscar o estado atual pela API.                                                                                      |
| ADR-10 | Usar notificações confiáveis via **outbox** para eventos que não podem se perder, como aviso ao cliente ou comunicação entre módulos. | Evita publicar uma notificação antes de a transação do pedido estar confirmada.                                                                                                                            |
| ADR-11 | Exigir conectividade para concluir pedidos e atualizar operação.                                                                      | Corresponde à decisão online-only e evita sincronização ambígua de pedidos ou estoque. Rascunhos locais podem ser avaliados separadamente.                                                                 |
| ADR-12 | Excluir integração com gateway de pagamento do MVP, mas manter espaço para um contexto de pagamentos futuro.                          | Evita acoplar pedidos a um provedor não escolhido. Ainda é preciso decidir se o caixa registrará pagamentos externos manualmente no MVP.                                                                   |

### Decisão de versão .NET

A especificação combina API em .NET 8 e cliente MAUI em .NET 10. Em **22 de outubro de 2026**, o suporte de .NET 8 está próximo do fim, previsto para **10 de novembro de 2026**. Recomendo alinhar API, MAUI e CI em **.NET 10**, sujeito à confirmação dos pacotes e workloads necessários.

## 3. Ajustes necessários ao modelo de domínio

1. **Filial:** incluir `Branch` e associar filial a pedidos, disponibilidade e preço do catálogo, estoque, entregas, usuários e operações. Dados compartilháveis, como produtos, podem ter configurações específicas por filial.
2. **Produto e pizza:** modelar tamanho, sabores selecionados, fração de cada sabor, borda e adicionais como uma composição validada pelo servidor. O limite de sabores deve ser uma regra configurável por tamanho/produto, não uma constante “3 ou 4”.
3. **Preço ponderado:** para sabores com frações \(f_i\) e preço por tamanho \(p_i\), calcular o componente de sabores como `Σ(f_i × p_i)`, exigindo que as frações totalizem 1. Somar borda e adicionais aplicáveis; aplicar taxas e descontos segundo regras explícitas; arredondar em ponto definido. O valor final deve ser persistido no pedido.
4. **Estoque:** acrescentar `Ingredient`, `Recipe`, `StockBalance`, `StockMovement` e `StockReservation`. As receitas devem definir quantidades e unidades; movimentações devem manter histórico, não apenas sobrescrever saldo.
5. **Pedido:** permitir cliente opcional para atendimento de balcão, se essa for a regra desejada. Guardar endereço de entrega como snapshot no pedido para que alterações posteriores no cadastro do cliente não modifiquem uma venda já realizada.
6. **Atendimento:** distinguir os canais `Counter`, `DineIn`, `Delivery` e `Takeout`. Para consumo em mesa, incluir mesa ou sessão de atendimento; para retirada, manter instruções e identificação de quem retirou, se exigidas.
7. **Identidade:** separar contas de clientes das contas de funcionários. Permissões de funcionários devem ser verificadas por função e filial autorizada; não depender apenas de ocultar telas no app.
8. **Dinheiro e horários:** usar tipo monetário decimal com moeda explícita, datas persistidas em UTC e conversão para o fuso da filial na apresentação.
9. **Histórico:** registrar ator, filial, data, transição e motivo de alterações relevantes de status, estoque e cancelamento.

## 4. Ciclo de vida recomendado do pedido

Não recomendo manter `Ready / Out for Delivery` como um único estado. São situações distintas, e entrega possui ciclo próprio.

**Pedido:**  
`Pending → Confirmed → Preparing → Ready → Completed`  
Cancelamento: permitido apenas nas transições e condições autorizadas; registrar responsável e motivo.

**Entrega, quando aplicável:**  
`PendingAssignment → Assigned → Dispatched → Delivered`

Regras recomendadas:

- Pedidos de entrega só podem ser concluídos depois de `Delivered`.
- Pedidos de retirada ou mesa podem ir de `Ready` para `Completed`.
- Uma entrega não deve voltar silenciosamente para um estado anterior; correções devem ser registradas.
- Estado de pagamento não altera automaticamente o estado operacional do pedido.
- Uma transição inválida deve resultar em erro de conflito, sem alterar o pedido.

## 5. Requisitos funcionais prioritários

| ID    | Requisito                                                                                                           |
| ----- | ------------------------------------------------------------------------------------------------------------------- |
| RF-01 | Administrar filiais, acesso por filial, usuários e permissões.                                                      |
| RF-02 | Cadastrar categorias, produtos, tamanhos, preços, sabores, bordas e disponibilidade.                                |
| RF-03 | Configurar preços e disponibilidade de catálogo por filial, caso essa política seja aprovada.                       |
| RF-04 | Criar pedidos de balcão, mesa, entrega e retirada, com ou sem cliente conforme o canal.                             |
| RF-05 | Configurar pizzas com tamanho, sabores fracionados, borda e adicionais, calculando o total no servidor.             |
| RF-06 | Consultar endereço e contato do cliente e registrar snapshots necessários no pedido.                                |
| RF-07 | Exibir pedidos da filial no KDS e aplicar apenas transições de estado autorizadas.                                  |
| RF-08 | Controlar insumos por receitas, reservas, consumo, perdas, ajustes e histórico de movimentação.                     |
| RF-09 | Atribuir entregas a entregadores autorizados e acompanhar despacho e conclusão.                                     |
| RF-10 | Manter trilha de auditoria para alterações operacionais e administrativas.                                          |
| RF-11 | Permitir ao cliente consultar o catálogo e acompanhar seus próprios pedidos, sem acesso a dados de outros clientes. |
| RF-12 | Disponibilizar relatórios operacionais por filial, com escopo e métricas definidos antes da implementação.          |

## 6. Histórias de usuário e critérios de aceite

### US-01 — Registrar pedido com preço confiável

**Como** atendente, **quero** registrar um pedido com itens e opções, **para que** a cozinha receba uma venda consistente com o preço vigente.

```gherkin
Cenário: Criar pedido de retirada
  Dado que estou autenticado e autorizado na filial
  E o produto está disponível nessa filial
  Quando envio um pedido de retirada com itens válidos
  Então a API recalcula os preços no servidor
  E registra um snapshot dos itens e valores no pedido
  E retorna o pedido criado e sua localização
```

### US-02 — Calcular pizza com vários sabores

**Como** atendente ou cliente, **quero** combinar sabores em frações, **para que** o preço reflita as proporções escolhidas.

```gherkin
Cenário: Calcular uma pizza com dois sabores
  Dado que os sabores têm preço configurado para o tamanho escolhido
  E suas frações somam 100%
  Quando adiciono os sabores e uma borda válida
  Então o preço dos sabores é calculado proporcionalmente às frações
  E o preço da borda é acrescentado conforme sua regra
  E a API rejeita frações inválidas ou que não totalizem 100%
```

### US-03 — Atualizar pedido na cozinha

**Como** profissional de cozinha, **quero** ver pedidos confirmados da minha filial e avançar seu estado, **para que** atendimento e cliente acompanhem o preparo.

```gherkin
Cenário: Avançar pedido para preparo
  Dado que o pedido está confirmado e pertence à filial do usuário
  Quando a cozinha inicia o preparo
  Então o pedido passa para “Preparing”
  E a atualização fica registrada com usuário e horário
  E os clientes conectados recebem a atualização após a gravação
```

### US-04 — Reservar e consumir estoque

**Como** responsável pelo estoque, **quero** que pedidos reservem os ingredientes de suas receitas, **para que** a disponibilidade não ultrapasse o saldo real.

```gherkin
Cenário: Confirmar pedido com estoque suficiente
  Dado que os ingredientes da receita estão disponíveis na filial
  Quando o pedido é confirmado
  Então o sistema reserva as quantidades necessárias
  E registra as movimentações de estoque correspondentes
  Quando o pedido começa a ser preparado
  Então o sistema registra o consumo conforme a política definida
```

### US-05 — Impedir confirmação sem estoque

```gherkin
Cenário: Estoque insuficiente
  Dado que um ingrediente necessário está sem saldo suficiente
  Quando tento confirmar o pedido
  Então o sistema não confirma o pedido
  E informa qual item ou ingrediente impediu a operação
  E não deixa reserva parcial inconsistente
```

### US-06 — Restringir acesso por filial

```gherkin
Cenário: Usuário tenta consultar pedido de outra filial
  Dado que o usuário não tem acesso à filial do pedido
  Quando solicita o pedido por identificador
  Então o sistema não revela seus dados
  E registra a tentativa conforme a política de auditoria
```

### US-07 — Acompanhar entrega

```gherkin
Cenário: Entregador conclui entrega atribuída
  Dado que a entrega está atribuída ao entregador autenticado
  Quando ele registra a entrega como concluída
  Então a entrega passa para “Delivered”
  E o horário e o responsável ficam registrados
  E o pedido passa para “Completed”
```

## 7. Requisitos não funcionais

- **Segurança e privacidade:** autenticação com tokens de acesso e refresh tokens revogáveis; autorização no backend; mínimo privilégio; isolamento por filial e por cliente; proteção de dados pessoais conforme a LGPD.
- **Desempenho:** definir carga esperada e SLO antes de produção. Como metas iniciais a validar, considerar p95 inferior a 1 s para consultas comuns e inferior a 2 s para gravação de pedido, sob carga acordada.
- **Atualizações em tempo real:** medir o intervalo entre confirmar o pedido e sua exibição no KDS; a API deve permitir ressincronização após queda de conexão.
- **Confiabilidade:** operações de pedido e reserva de estoque precisam ser atômicas; impedir duplicidade por repetição de requisição com chave de idempotência nos pontos críticos.
- **Rastreabilidade:** logs estruturados com correlação por requisição, sem registrar tokens ou dados pessoais desnecessários.
- **Acessibilidade e localização:** interface em português brasileiro; código, identificadores, commits e configuração em inglês, conforme solicitado.
- **Cancelamento e falhas de rede:** como a operação é online-only, o app deve comunicar falhas claramente e não apresentar um pedido como criado antes da confirmação da API.

## 8. Decisões de implementação e contrato HTTP

- Organizar os casos de uso em módulos com fronteiras explícitas, sem introduzir microserviços ou abstrações genéricas antes de necessidade comprovada.
- Usar DTOs como `positional records` e propagar `CancellationToken` nas operações de I/O.
- Nas consultas, projetar para DTO diretamente na consulta e usar `AsNoTracking`.
- Padronizar erros com `ProblemDetails` — a especificação chama de RFC 7807; pode-se adotar a implementação atualizada baseada na RFC 9457, mantendo formato consistente.
- Definir: `201 Created` com `Location` para criação; `200 OK` para consultas e atualizações com corpo; `400` para entrada inválida; `422` para regra de negócio, se essa distinção for adotada; `404` para recurso ausente; `409` para conflito de estado ou concorrência; `401/403` para autenticação e autorização.
- Escolher **um** estilo de cliente HTTP no MAUI — Refit ou clientes tipados com `HttpClientFactory` — evitando duplicidade.
- Manter o frontend MVVM; as telas não devem conter regras de negócio nem chamadas diretas à API.

## 9. CI/CD e organização das entregas

A pipeline proposta precisa ser ampliada para cobrir os clientes e a configuração multi-filial:

1. Validar compilação da API, análise estática e testes.
2. Executar testes de integração relevantes com SQL Server.
3. Compilar Android em runner compatível com workloads MAUI.
4. Compilar Windows em runner Windows; `ubuntu-latest` não substitui esse build.
5. Validar migrations e configuração de ambiente sem incluir segredos no repositório.
6. Manter validação de PR para `develop` e `feature/**`, com artefatos separados por plataforma quando aplicável.

## 10. Fora do MVP recomendado

- Gateway/adquirente de pagamento e conciliação financeira.
- Emissão fiscal, integração com PDV fiscal e regras tributárias.
- Fidelidade, cupons complexos, marketing e relatórios analíticos avançados.
- Rastreamento GPS, roteirização e integração com mapas.
- Criação e confirmação de pedidos sem conexão.
- Separação do backend em microserviços.

## 11. Pontos que ainda precisam de definição

As respostas recebidas resolvem decisões importantes, mas ainda faltam regras comerciais que não devem ser inferidas silenciosamente:

1. **Filiais:** catálogo e preços são centralizados com exceções locais, ou cada filial administra preços integralmente?
2. **App de clientes:** pedidos exigem conta, aceitam compra como visitante ou ambos? O app de clientes será apenas Android ou também Windows?
3. **Pagamentos no MVP:** embora gateway esteja fora do escopo, o caixa precisa registrar manualmente pagamentos feitos fora do sistema?
4. **Estoque:** reserva na confirmação e baixa ao iniciar preparo são aceitáveis? Cancelamentos após o preparo iniciado exigem ajuste manual?
5. **Pedidos de cliente:** cliente pode cancelar? Até qual estado? Quais critérios liberam reembolso, caso pagamentos sejam adicionados no futuro?
6. **Taxas e descontos:** como calcular entrega, descontos, arredondamentos e eventuais taxas? A política deve ser especificada antes de fixar o contrato de preço.
7. **Operação:** quais são as filiais, fusos horários, volumes esperados e metas de disponibilidade? Esses dados fecham os SLOs de performance e disponibilidade.

## Pacote de handoff para arquitetura

O próximo passo de arquitetura deve transformar este material em: mapa dos contextos e suas dependências; diagrama de entidades e fluxo de pedido/estoque; contrato OpenAPI versionado; política de acesso por filial; modelo de catálogo e preço ponderado; estratégia transacional para pedido e reserva; e ADRs para a versão .NET, SQL Server, atualização em tempo real e estratégia de CI por plataforma.
