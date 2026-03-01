# API de Gestión Financiera (Financial Management API)

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue?style=for-the-badge)

API REST en **ASP.NET Core 8** para **gestión financiera personal** (base técnica). Enfocada a demostrar una estructura mantenible con **Clean Architecture**, persistencia en **SQL Server (LocalDB / SQL Express)** y documentación con **Swagger**.

> Nota: actualmente **no hay demo online**. El proyecto se ejecuta en local con SQL Server Express/LocalDB.

---

## Qué incluye
- API en .NET 8 con estructura por capas (Clean Architecture).
- Persistencia con **Entity Framework Core** y **SQL Server**.
- Swagger/OpenAPI para probar endpoints.
- Configuración para ejecutar en local.

## Objetivo del repositorio
Este repositorio está orientado a mostrar:
- organización del código (separación Domain / Application / Infrastructure / Presentation),
- patrones base para escalar el proyecto,
- integración limpia con base de datos SQL Server.

## Arquitectura
- **Domain:** entidades y reglas de negocio (finanzas).
- **Application:** casos de uso y contratos (interfaces/DTOs).
- **Infrastructure:** EF Core + SQL Server (repositorios/implementaciones).
- **Presentation:** API Controllers + Swagger.

> Algunas piezas (por ejemplo CQRS completo, autenticación, tests automatizados) se irán añadiendo progresivamente.

---

## Requisitos
- **.NET SDK 8**
- **SQL Server Express / LocalDB** (o SQL Server normal)
- (Opcional) **Visual Studio 2022** o VS Code

---

## Cómo ejecutar en local
## 1)Clona el repositorio

git clone https://github.com/Nain9Dev/API-Gestion-Financiera.git

cd API-Gestion-Financiera

## 2)Configura la conexión a SQL Server
Edita appsettings.json (o appsettings.Development.json) con tu connection string.

Ejemplo (SQL Server Express):

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=GestionFinancieraDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}

## 3)Aplica migraciones

dotnet ef database update

## 4)Ejecuta la API

dotnet run

## 5)Abre Swagger
Normalmente:

https://localhost:<puerto>/swagger

Roadmap (en desarrollo)

Endpoints completos de dominio financiero (categorías, ingresos, gastos, presupuestos).

Autenticación (JWT) y autorización.

Validaciones y manejo de errores consistente.

Tests (unitarios e integración).

Docker Compose (API + SQL Server).

## Autor

Aitor Nain Mendoza Vallejo

Desarrollador Backend .NET | Estudiante de Ingeniería Informática

Contacto: contact@naindev.com
Portfolio: https://www.naindev.com
