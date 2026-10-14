# AGENTS.md — canonical constitution for this repository

This file governs every automated agent and every human contributor working inside
`API-Gestion-Financiera`. It is the highest-priority instruction set in this directory tree.

## 1. What this repository is

An **Insurance Policy Operations API**: a .NET 10 modular monolith that demonstrates reliable
policy lifecycle operations with organization-scoped authorization, optimistic concurrency and an
append-only audit trail on SQL Server.

It is also a **commercial proof asset for [naindev.com](https://www.naindev.com)**. The public demo
at `/demo/` is linked from the portfolio as *"API de Operaciones de Pólizas"* and is, for many
visitors, the first executable evidence of the owner's engineering. A broken, slow or overstated
demo damages the portfolio more than a missing feature would.

It is **not** a finished SaaS, and no claim may exceed the verified evidence recorded in `docs/`.

## 2. Authority order

1. Safety, legality and data preservation.
2. Approved ADRs in `docs/decisions/` within their stated scope.
3. `docs/business-rules.md` — approved rules and the public contract.
4. `docs/project-context.md` — scope, readiness gates and boundaries.
5. Versioned code, migrations and configuration — authoritative for *implemented* behaviour.
6. `specs/NNN-*/` — the in-flight change currently being built.
7. `docs/roadmap.md` — ordering only; it approves nothing by itself.

If two approved sources conflict and neither has precedence, stop and ask the owner.

## 3. Non-negotiable boundaries

- **No real personal data.** Every value in every demo, test and fixture is synthetic.
- **No paid infrastructure.** The authorized incremental budget is 0 EUR. Azure SQL must stay on
  the free offer with `AutoPause`; App Service must stay on F1.
- **No public issuance of tokens.** `dotnet user-jwts` is a local development issuer only.
- **No client-selected organization or actor.** Both come from validated JWT claims, always.
- **No silent business-behaviour change.** Contracts in `docs/business-rules.md` change only
  through an ADR the owner has moved to `Approved`.
- **No automatic state-changing retry** of a lifecycle command without a freshly read ETag.
- **No claim without evidence.** If `docs/` says a number, a run or a test produced it.

## 4. How to change this repository

1. Start in `specs/NNN-feature-name/spec.md`. Requirements use the form
   *"When X, the system shall Y"*. No code before the requirement exists.
2. Record significant design decisions as an ADR in `docs/decisions/`, state `Proposed`.
   **Only the owner moves an ADR to `Approved`.**
3. Write the test before the implementation. A requirement without a test that demonstrates it is
   not done, whatever the code says.
4. Keep the change minimal, correct and verifiable. Prefer the smallest diff that satisfies the
   requirement.
5. Update `docs/traceability.md` so the requirement points at its test.
6. Update `docs/changelog.md` and the affected documents in the same change as the code.
7. Hand off: before the session ends, record done / verified / next / blocked in the active
   `specs/NNN-*/tasks.md`. The next session starts by reading `docs/`, never from memory.

## 5. Layering

```text
PolicyOperations.Api            composition, HTTP, JWT, ETag, OpenAPI, public demo façade
  -> PolicyOperations.Application   use cases, ports, DTOs, orchestration
      -> PolicyOperations.Domain    invariants, lifecycle, transitions
  -> PolicyOperations.Infrastructure EF Core, mappings, migrations, provider knowledge
```

- `Domain` references nothing. No ASP.NET Core, no EF Core, no SQL, no JWT.
- `Application` defines the ports it needs; it never references a provider type.
- Provider-specific knowledge (SQL error numbers, `rowversion`, retry strategy) lives in
  `Infrastructure`.
- The API layer presents; it does not compute business figures and does not reach the database
  directly except through the demo pruner, which owns only housekeeping.
- No MediatR, AutoMapper, generic repository or validation framework. This is a deliberate,
  recorded decision (ADR-002), not an oversight.

## 6. Language rules

Everything that lands in the repository is written in **English**: code, identifiers, file and
folder names, branches, commits, comments, docstrings, documentation, tests, fixtures, technical
logs, schemas, migrations and configuration.

Two exceptions:

1. Text shown to the end user — the `/demo/` page — is written in Spanish, the product's approved
   locale.
2. Legal or tax terms without an exact equivalent (`IRPF`, `NIF`) stay as values, with an English
   comment explaining them.

## 7. Commits

Conventional Commits, English subject: `<type>(<scope>): <description>`.
Types: `feat`, `fix`, `refactor`, `perf`, `docs`, `test`, `build`, `ci`, `chore`.
Contract breaks use `!`. No dates in messages; git records them.
Commits are authored solely by the repository owner; no co-author or tooling trailers.

Do not commit or push unless the owner explicitly asks.

## 8. Verification gate

A change is finished only when all of these pass and the result is reported honestly:

```bash
dotnet build ./GestionFinanciera/GestionFinanciera.sln --configuration Release
dotnet format ./GestionFinanciera/GestionFinanciera.sln --verify-no-changes
dotnet test ./GestionFinanciera/GestionFinanciera.sln --configuration Release --no-build
```

The SQL Server suites need `POLICY_OPERATIONS_TEST_SQLSERVER`; without it they skip, and a skipped
suite is not evidence. Warnings are errors. Reporting "tests pass" while a suite was skipped is a
governance failure, not a shortcut.

## 9. Task labels

Every task in `specs/NNN-*/tasks.md` carries one label:

- `[A]` the agent completes it alone.
- `[M]` mixed: the agent does its part, the owner closes it.
- `[H]` owner only: accounts, credentials, payment, terms, business decisions, ADR approval.

If an `[M]` or `[H]` task blocks progress, stop and say so. Never invent a credential, never
simulate an external dependency silently, and never mark a dependent task done.
