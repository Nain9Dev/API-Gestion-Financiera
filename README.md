# Insurance Policy Operations API

Este repositorio empezó como un CRUD pequeño y lo he convertido en una demo backend que se puede recorrer de principio a fin. La API gestiona el ciclo de una póliza, separa los datos de cada organización y deja un historial auditable en SQL Server.

El objetivo no es aparentar que ya existe un SaaS terminado. Es enseñar, con código y pruebas que se pueden ejecutar, cómo trabajo con .NET, C#, diseño de APIs, reglas de negocio y persistencia relacional.

> Estado: **demo técnica pública y verificada con datos sintéticos**. Puede probarse por HTTPS sin instalar nada. No está autorizada para clientes ni datos personales reales.

## Capacidades implementadas

- Monolito modular: Domain, Application, Infrastructure y API.
- Creación de pólizas en estado `Draft` con importe y moneda ISO 4217.
- Ciclo `Draft -> Active -> Cancelled`, incluida cancelación directa de Draft.
- Datos mínimos de activación: referencia opaca del asegurado y periodo de cobertura.
- JWT Bearer, roles `PolicyReader` y `PolicyOperator`.
- Organización derivada del claim confiable `organization_id`.
- Lecturas y unicidad de `PolicyNumber` aisladas por organización.
- SQL Server `rowversion`, ETag e `If-Match` para evitar escrituras obsoletas.
- Historial de transiciones con actor, UTC, correlación y motivo.
- Problem Details seguro con stable error codes.
- EF Core 10 y migrations que se niegan a inventar ownership o moneda para datos antiguos.
- Swagger visual, colección `.http` y endpoint de salud del proceso.
- Página de demo sin registro ni descargas que ejecuta un escenario sintético completo.
- Rate limiting, retención de 24 horas y organización dedicada para la demo pública.
- Readiness independiente para comprobar la conexión con SQL Server.
- Tests de dominio, API, autorización, aislamiento y migrations contra SQL Server 2025 real.

## Arquitectura

```text
HTTP + JWT
  -> PolicyOperations.Api
      -> PolicyOperations.Application
          -> PolicyOperations.Domain
      -> PolicyOperations.Infrastructure
          -> EF Core 10
              -> SQL Server
```

La API es el composition root. Application define casos de uso y puertos concretos; Infrastructure implementa persistencia; Domain no depende de ASP.NET Core ni EF Core.

## Endpoints

| Método | Ruta | Autorización | Comportamiento |
|---|---|---|---|
| `GET` | `/health` | Anonymous | Liveness del proceso; no comprueba SQL Server |
| `GET` | `/health/ready` | Anonymous | Readiness de la conexión con SQL Server |
| `POST` | `/api/v1/demo/run` | Anonymous cuando se habilita | Ejecuta el escenario sintético fijo de cinco pasos |
| `POST` | `/api/v1/policies` | `PolicyOperator` | Crea una póliza `Draft` |
| `GET` | `/api/v1/policies/{policyId}` | Reader u Operator | Recupera una póliza de la organización |
| `GET` | `/api/v1/policies` | Reader u Operator | Lista paginada, máximo 100 elementos |
| `POST` | `/api/v1/policies/{policyId}/activate` | `PolicyOperator` + `If-Match` | Activa un Draft completo |
| `POST` | `/api/v1/policies/{policyId}/cancel` | `PolicyOperator` + `If-Match` | Cancela un Draft o Active |
| `GET` | `/api/v1/policies/{policyId}/transitions` | Reader u Operator | Muestra el historial de estados |

## Demo visual local

La guía completa está en [docs/local-demo.md](docs/local-demo.md). Resumen:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=.\NAINCONFIGURATOR;Database=PolicyOperationsLocalDemo;Integrated Security=True;TrustServerCertificate=True;Encrypt=False"
$env:POLICY_OPERATIONS_MIGRATIONS_SQLSERVER = $env:ConnectionStrings__DefaultConnection

dotnet tool restore
dotnet restore .\GestionFinanciera\GestionFinanciera.sln
dotnet tool run dotnet-ef database update --project .\GestionFinanciera\PolicyOperations.Infrastructure --startup-project .\GestionFinanciera\PolicyOperations.Infrastructure --configuration Release

dotnet user-jwts create --project .\GestionFinanciera\PolicyOperations.Api\PolicyOperations.Api.csproj --name demo-operator --role PolicyOperator --role PolicyReader --claim organization_id=11111111-1111-1111-1111-111111111111 --valid-for 8h

dotnet run --project .\GestionFinanciera\PolicyOperations.Api --configuration Release --launch-profile https
```

Abre [https://localhost:7024/swagger](https://localhost:7024/swagger) o [http://localhost:5047/swagger](http://localhost:5047/swagger), selecciona **Authorize** y pega el token local.

En `If-Match`, copia el ETag completo tal como aparece en la cabecera de respuesta, incluidas las comillas: `"AAAAAAAAAAE="`. Pegar solo `AAAAAAAAAAE=` no representa un ETag HTTP válido y la API responderá con `etag_invalid`.

La base `PolicyOperationsLocalDemo` fue creada y migrada en el equipo verificado el 2026-08-02. Contiene exclusivamente evidencia sintética del smoke test local.

Requests alternativas: [PolicyOperations.Api.http](GestionFinanciera/PolicyOperations.Api/PolicyOperations.Api.http).

## Demo de un clic

La página `/demo/` llama a una única operación sin cuerpo y muestra el resultado paso a paso:

```text
201 Draft -> 200 Active -> 412 stale ETag -> 200 Cancelled -> 200 audit
```

El recorrido se ha ejecutado desde un navegador contra SQL Server 2025 local y devolvió dos transiciones de auditoría. La demo no pide un token, no permite introducir texto y elimina registros sintéticos antiguos de su organización dedicada.

Abre [Probar API](https://nain-policy-demo-api.azurewebsites.net/demo/) para ejecutar el recorrido. La configuración de Azure, los límites gratuitos y la evidencia observada están en [docs/public-demo.md](docs/public-demo.md).

## Quality gate verificado

- Build Release: 0 warnings y 0 errores.
- 22 tests de dominio.
- 17 tests de API, autorización, organización, demo pública, readiness y OpenAPI.
- 4 tests de migrations y recuperación segura.
- 43 tests en la suite; 21 utilizan SQL Server real.
- EF Core sin cambios de modelo pendientes de migration.
- Auditoría NuGet sin vulnerabilidades conocidas en paquetes directos o transitivos.
- Smoke test real: `Draft -> Active -> Cancelled`, dos transiciones persistidas mediante JWT local.

## Límites actuales

- El JWT de `dotnet user-jwts` es exclusivamente para desarrollo local.
- La operación pública de demo no sustituye autenticación ni permite trabajar con datos elegidos por el visitante.
- Un piloto requiere un issuer OpenID Connect aprobado y configuración operativa.
- No existe administración de organizaciones, invitaciones o usuarios.
- No se han implementado retención/export del audit trail, restauración probada ni alertas operativas para clientes. La demo sí avisa cuando Azure SQL Free se acerca a su cuota.
- No existe evaluación de riesgo, billing, integraciones ni multi-región.
- No se permiten datos personales reales ni exposición de los puertos locales a internet.
- La capacidad objetivo todavía no tiene un load test reproducible.

## Coste y licencia

La fase local usa herramientas gratuitas: .NET, paquetes open source, MIT para el repositorio y SQL Server 2025 Standard Developer. La demo pública está desplegada en Azure App Service F1 y Azure SQL Free. La base tiene `AutoPause` al agotar la cuota y no puede continuar como uso de pago; una alerta por correo avisa cuando queda aproximadamente el 1 %. El coste incremental autorizado sigue siendo 0 EUR y el límite aprobado antes de exigir evidencia comercial es 40 horas adicionales desde el 2026-08-02.

SQL Server Standard Developer es gratuito para desarrollo/pruebas, pero no está licenciado para producción. Producción requerirá una decisión explícita sobre base de datos, hosting, backups, restauración, privacidad y soporte.

## Documentación

- [Mapa y autoridad](docs/README.md)
- [Contexto y estado](docs/project-context.md)
- [Reglas de negocio y contratos](docs/business-rules.md)
- [Arquitectura](docs/architecture.md)
- [Roadmap](docs/roadmap.md)
- [Demo local](docs/local-demo.md)
- [Demo pública de un clic](docs/public-demo.md)
- [Economic brief](docs/economic-brief.md)
- [ADR-001: product direction](docs/decisions/ADR-001-product-direction.md)
- [ADR-002: runtime and architecture](docs/decisions/ADR-002-runtime-and-architecture.md)
- [ADR-003: lifecycle and security](docs/decisions/ADR-003-lifecycle-security-and-organization-boundary.md)
- [ADR-004: repository license](docs/decisions/ADR-004-repository-license.md)
- [ADR-005: public demo and free hosting](docs/decisions/ADR-005-public-demo-and-free-hosting.md)

## Autor

Aitor Nain Mendoza Vallejo — Backend .NET Developer

[naindev.com](https://www.naindev.com/) · contact@naindev.com
