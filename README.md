# BytePizza — Multi-Agent Engineering Sandbox

> **Projeto de Estudos & Laboratório de Engenharia com Agentes de IA**  
> Este repositório é um ambiente de testes práticos para validação de desenvolvimento orientado a múltiplos agentes especialistas (Arquitetura, Coder, QA, DevOps, Reviewer) utilizando arquivos de instrução Markdown (`.prompt.md`) integrados ao GitHub Copilot / VS Code.

---

## Objetivo do Projeto
Demonstrar a construção de uma solução enterprise end-to-end em C# / .NET simulando o fluxo de trabalho de um time de desenvolvimento real, onde cada etapa do ciclo de vida de software é orquestrada por uma persona autônoma de IA.

---

## Stack Tecnológica
- **Target Framework:** .NET 9 / C# 13
- **Backend:** ASP.NET Core Web API (Clean Architecture + DDD + CQRS)
- **Client Mobile/Desktop:** .NET MAUI com MVVM (`CommunityToolkit.Mvvm`)
- **Persistência:** Entity Framework Core (PostgreSQL / SQL Server) + Redis
- **Qualidade & Testes:** xUnit, Moq, FluentAssertions, Bogus
- **Automação & DevOps:** GitHub Actions (CI/CD) + Docker

---

## Ecossistema de Agentes (.github/prompts/)
O ciclo de vida do software neste repositório é regido pelos seguintes agentes especializados:

| Agente | Arquivo | Papel Principal |
| :--- | :--- | :--- |
| **Product Owner** | `00-po.prompt.md` | Refinamento de Requisitos, User Stories e BDD (Gherkin). |
| **Solution Architect** | `01-architect.prompt.md` | Modeling DDD, Bounded Contexts, OpenAPI e Diagramas Mermaid. |
| **Coder Specialist** | `02-coder.prompt.md` | Implementação C# 13, Rich Domain Models e .NET MAUI. |
| **Code Reviewer** | `03-reviewer.prompt.md` | Auditoria Estática, OWASP Top 10 e Performance no EF Core. |
| **QA & Test Engine** | `04-tester.prompt.md` | Testes Unitários e Integração com xUnit e Bogus. |
| **Committer Agent** | `05-committer.prompt.md` | Formatador de Conventional Commits (Inglês) e PRs. |
| **DevOps Engine** | `06-devops.prompt.md` | Workflows YAML do GitHub Actions e Dockerfiles. |

---

## Como Utilizar os Agentes no VS Code
Para acionar qualquer agente nas sessões de chat, utilize a sintaxe de arquivo relativo:
```text
Atue como #file:.github/prompts/00-po.prompt.md e analise o requisito X.
```
## Licença & Uso
Projeto estritamente educacional para fins de mentoria, aprendizado de arquitetura .NET 9 e orquestração de IAs gerativas em ambiente de desenvolvimento.
