# Changelog

Todas as mudanças notáveis deste projeto serão documentadas neste arquivo.

O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/), e este projeto adota [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [Unreleased]

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
