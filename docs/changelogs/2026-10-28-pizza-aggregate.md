# BytePizza — Registro técnico do agregado Pizza (28/10/2026)

## Visão geral

Esta entrega acrescenta ao domínio a composição de uma pizza com tamanho, sabores fracionados e borda, além de registrar a regra na especificação de produto e ampliar a suíte unitária. O objetivo é manter as invariantes de composição e o cálculo monetário na camada de domínio, sem dependência de infraestrutura.

## O que mudou e por quê

| Arquivo | Tipo | Mudança e motivo |
|---|---|---|
| `src/BytePizza.Domain/Entities/Pizza.cs` | Criado; posteriormente documentado | Agregado com construtor privado e fábrica `Create`, validação de tamanho, sabores, borda, limites, total exato das frações e consistência de moeda. `CalculatePrice` soma os preços ponderados dos sabores e o adicional da borda antes do arredondamento final via `Money`. |
| `src/BytePizza.Domain/Entities/PizzaFlavor.cs` | Criado | Modelo imutável que associa nome, preço (`Money`) e fração (`FlavorFraction`) de um sabor. |
| `src/BytePizza.Domain/Entities/PizzaCrust.cs` | Criado | Modelo imutável que associa nome e preço adicional da borda recheada. |
| `src/BytePizza.Domain/Enums/PizzaSize.cs` | Criado | Enumeração Small, Medium e Large, usada para aplicar limites de sabores conforme o tamanho. |
| `tests/BytePizza.Tests.Unit/Entities/PizzaTests.cs` | Criado; posteriormente ampliado | Testes xUnit com FluentAssertions para sucesso e falha da fábrica, limites, frações exatas, dados ausentes/inválidos, incompatibilidade de moeda, composição de preço, arredondamento e overflow. |
| `docs/00-po-specification.md` | Atualizado | Removidas transcrições de execução e JSONs que precediam o cabeçalho principal; registrados os limites padrão de sabores por tamanho, com possibilidade de configuração posterior no catálogo. |
| `CHANGELOG.md` | Atualizado | Adicionado o resumo da entrega à seção `[Unreleased]`, seguindo Keep a Changelog. |

### Limites definidos

| Tamanho | Máximo de sabores |
|---|---:|
| Small | 2 |
| Medium | 3 |
| Large | 4 |

Esses valores são limites iniciais explícitos. A especificação permite que configurações do catálogo por produto/tamanho os substituam futuramente.

### Cálculo do preço

Para cada sabor, o domínio multiplica o preço pela fração correspondente, soma todas as parcelas sem arredondar individualmente e adiciona o preço da borda:

```text
total = Σ(preço do sabor × fração) + adicional da borda
```

O resultado passa por `Money.Create`, preservando a política central de moeda e arredondamento. Manter a precisão decimal até a soma evita que frações periódicas, como três parcelas de 1/3, sejam arredondadas separadamente e alterem o total.

## Decisões arquiteturais aplicadas

1. **Agregado com invariantes próprias:** `Pizza.Create` impede a criação de uma composição incompleta ou inválida e retorna `Result<Pizza>` para falhas esperadas.
2. **Imutabilidade:** a entidade expõe uma coleção somente para leitura; os sabores e a borda são modelos imutáveis que reutilizam os Value Objects existentes.
3. **Aritmética racional exata para frações:** a validação delega a `FlavorFraction.ValidateTotal`, que verifica igualdade exata sem tolerância de ponto flutuante.
4. **Cálculo monetário centralizado:** preços devem compartilhar a mesma moeda; arredondamento e escala são delegados ao Value Object `Money`.
5. **Testes de domínio isolados:** xUnit e FluentAssertions verificam as regras sem banco, rede ou mocks de infraestrutura.

## Histórico e integração

A auditoria Git identificou estes commits atômicos na branch `feature/domain-pizza-aggregate`:

| Commit | Mensagem |
|---|---|
| `d3c89d4` | `docs(po): sanitize specification document and align flavor limits` |
| `2747810` | `test(domain): add unit test suite for Pizza aggregate root` |
| `38c611e` | `feat(domain): implement Pizza aggregate root and supporting domain models` |

O [PR #10](https://github.com/DanielASantos7/BytePizza/pull/10) foi integrado em `develop`. O check **Build and test** do workflow **CI Quality Gate** concluiu com sucesso na revisão integrada.

Os comentários explicativos em `Pizza.cs` e a ampliação subsequente dos testes foram alterações locais posteriores ao conjunto de commits acima; não são atribuídas ao PR já integrado neste registro.

## Métricas de qualidade e limites da evidência

- Suíte unitária completa executada após a ampliação local: **49 aprovados, 0 falhas, 0 ignorados**.
- Testes do agregado Pizza: **22 casos aprovados**, incluindo os casos parametrizados por tamanho/moeda.
- `git diff --check`: sem erros de whitespace na auditoria.
- A coleta local de cobertura foi executada, mas não foi produzido um relatório percentual legível. Nenhum percentual de cobertura é declarado.
- A aprovação do CI refere-se ao check `Build and test` do PR #10; não representa o estado de execuções futuras ou de alterações locais posteriores.
