# Changelog

Todas as mudanças notáveis deste projeto serão documentadas neste arquivo.

O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/), e este projeto adota [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [Unreleased]

### Added

- Agregado `Pizza` com `PizzaFlavor`, `PizzaCrust` e `PizzaSize`, validando a composição e calculando o preço proporcional aos sabores mais o adicional da borda.
- Testes unitários do agregado Pizza para frações, limites por tamanho, dados inválidos, moedas, arredondamento e overflow.

### Changed

- Especificação de produto higienizada para remover transcrições de ferramentas e registrar os limites iniciais Small (2), Medium (3) e Large (4), substituíveis por configuração do catálogo.

### Fixed

- Removido conteúdo acidental de execução que precedia o título principal de `docs/00-po-specification.md`.

Para o contexto, as decisões e a auditoria da implementação Pizza, consulte o [registro técnico de 2026-10-28](docs/changelogs/2026-10-28-pizza-aggregate.md).

## [0.1.0] - 2026-10-23

### Added

- Camada `BytePizza.Domain` com `Entity<TId>`, marcador `IAggregateRoot` e tipos `Result` para representar falhas de domínio explicitamente.
- Value Objects imutáveis `Address`, `Money` e `FlavorFraction`, com validação, normalização e invariantes de endereço, dinheiro e divisão de pizza.
- Projeto de testes unitários xUnit com FluentAssertions cobrindo os primitives e Value Objects do domínio.
- Workflow GitHub Actions `CI Quality Gate` para restaurar, compilar em Release e executar os testes em PRs para `develop`/`main` e pushes para `develop`.
- Prompt especializado de documentação para auditoria de histórico, registros de release e manutenção deste changelog.

### Changed

- Fluxo de desenvolvimento organizado com integração das mudanças via branches de feature e pull requests para as branches de desenvolvimento e principal.

### Fixed

- Nenhuma correção isolada registrada nesta release; as validações de domínio e a CI foram introduzidas como funcionalidades e controles preventivos.

Para o histórico detalhado, decisões técnicas, escopo do pipeline e evidências de testes, consulte [o registro da release v0.1.0](docs/changelogs/2026-10-23-release-v0.1.0.md).
