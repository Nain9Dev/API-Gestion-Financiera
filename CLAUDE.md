@AGENTS.md

# Claude Code adjustments

`AGENTS.md` is the canonical constitution for this repository. This file adds only the details
specific to running Claude Code here.

## Environment

- Windows with PowerShell as the primary shell; a Bash tool is also available. Each takes its own
  syntax, so do not mix them in one command.
- Local SQL Server 2025 Standard Developer is reachable as `.\NAINCONFIGURATOR`. It is a
  development instance and must never be described or used as production infrastructure.
- Run the SQL suites with:

```powershell
$env:POLICY_OPERATIONS_TEST_SQLSERVER = "Server=.\NAINCONFIGURATOR;Integrated Security=True;TrustServerCertificate=True;Encrypt=False"
dotnet test .\GestionFinanciera\GestionFinanciera.sln --configuration Release
```

- Integration tests create and drop databases prefixed `PolicyOperationsTests_`. Never widen that
  prefix and never point the variable at a database holding data you did not create.

## Working style here

- Conversation with the owner is in Spanish; everything written into the repository is in English,
  except the visitor-facing `/demo/` page.
- Start from the conclusion or the diagnosis, then the evidence, then the detail.
- Flag real risks even when they contradict the request.
- Do not run the live Azure demo repeatedly: it consumes the free monthly allowance that keeps the
  portfolio link working.
