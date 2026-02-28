# API de Gestión Financiera (Financial Management API)

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue?style=for-the-badge)

## Sobre el Proyecto
Esta es una API RESTful diseñada para la gestión de pólizas de seguros y evaluación de riesgos. El objetivo principal de este repositorio es demostrar la implementación de patrones de diseño de nivel empresarial y código limpio.

## Arquitectura y Patrones (En Desarrollo)
Este sistema está siendo construido siguiendo los principios de **Clean Architecture** y **Domain-Driven Design (DDD)** para asegurar un alto desacoplamiento y escalabilidad:

* **Domain Layer:** Entidades de negocio centrales (Pólizas, Clientes, Scorings).
* **Application Layer:** Casos de uso e interfaces (CQRS pattern).
* **Infrastructure Layer:** Persistencia de datos mediante Entity Framework Core y SQL Server.
* **Presentation Layer:** Controladores API expuestos y documentados mediante Swagger.

## Tecnologías Principales
* **Framework:** ASP.NET Core 8 Web API
* **Base de Datos:** Microsoft SQL Server (T-SQL)
* **Testing:** Postman para validación de endpoints e integridad de datos.
* **Control de Versiones:** Git & GitHub Workflow.

## Autor
**Aitor Nain Mendoza Vallejo**
* Desarrollador Backend .NET | Estudiante de Ingeniería Informática
* Contacto: contact@naindev.com
* Portfolio: [www.naindev.com](https://www.naindev.com)