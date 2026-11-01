# Arquitectura de Timeblock

Timeblock será una aplicación web para gestionar la generación y programación
de horarios docentes. Está compuesta por una interfaz web, una API y almacenes
de datos para las operaciones del sistema y el análisis histórico.

## Componentes

- **Frontend:** aplicación de página única desarrollada con React, Vite y
  TypeScript. React Router gestiona la navegación. El código se organiza por
  funcionalidades, con componentes y servicios compartidos cuando sean
  necesarios.
- **API:** aplicación ASP.NET Core en .NET 10. Expone las operaciones del
  sistema mediante HTTP y JSON, y documenta sus endpoints con OpenAPI.
- **PostgreSQL:** almacén relacional operacional para la información y las
  transacciones del sistema. La persistencia se implementará con Entity
  Framework Core y Npgsql.
- **ClickHouse:** almacén analítico separado para el histórico de ocupación y
  asistencia.

En el entorno de desarrollo, PostgreSQL y ClickHouse se ejecutan en Docker; el
frontend y la API se ejecutan localmente.

## Organización del backend

El backend está dividido en cuatro proyectos:

- `Timeblock.Api` recibe las solicitudes y configura la aplicación.
- `Timeblock.Application` contiene los casos de uso.
- `Timeblock.Domain` contiene las entidades y reglas principales del negocio.
- `Timeblock.Infrastructure` implementa los detalles técnicos, como el acceso
  a datos.

Las dependencias apuntan hacia el dominio y la aplicación:

```text
Api            → Application
Api            → Infrastructure (composición de dependencias)
Infrastructure → Application y Domain
Application    → Domain
Domain         → ninguna otra capa del proyecto
```

Así, las reglas del negocio no dependen de la API ni de la tecnología de
persistencia.

## Convenciones de Application y la API

**Casos de uso.** Cada caso de uso es una clase de `Timeblock.Application`,
agrupada por módulo (`Personas`, `Catalogo`, `Matriculas`, `Bloques`,
`Horarios`), que implementa `ICasoDeUso`. `AddApplication()` registra
automáticamente, con ciclo de vida por solicitud, las clases concretas que
implementan esa interfaz, de modo que añadir un caso de uso no requiere editar
el registro de dependencias. Application define las abstracciones que necesita
(por ejemplo, repositorios) e Infrastructure las implementa; la API solo
conoce los casos de uso.

**Errores.** Las reglas del negocio lanzan `DomainException`. Su `Tipo`
(`Validacion`, `AutorizacionNegocio`, `NoEncontrado` o `Conflicto`) determina
el código HTTP (400, 403, 404 y 409). Cualquier otra excepción produce un 500
sin detalles internos, que se registran solo en el log. Cuando un caso de uso no
encuentra un recurso, lanza `DomainException` con `TipoErrorDominio.NoEncontrado`.

**Listados.** `PageRequest` normaliza `page` y `pageSize` (por defecto 1 y 20,
máximo 100) y `PagedResult<T>` es la respuesta común. En Infrastructure,
`ToPagedResultAsync` pagina una consulta de EF Core, que debe estar ordenada.

**Formato JSON.** Las propiedades usan `camelCase`, las enumeraciones se envían
como cadenas, las fechas como `YYYY-MM-DD` y las marcas de tiempo en UTC con
sufijo `Z`.

## Organización del frontend

El frontend es una aplicación web única desarrollada con React, Vite y
TypeScript. React Router se encarga de la navegación entre páginas. La
organización prevista separa la configuración general de las funcionalidades:

- `app/` contiene el arranque de la aplicación, las rutas y la configuración
  global.
- `features/` agrupa el código relacionado con cada funcionalidad, por ejemplo,
  la gestión de horarios o de bloques.
- `shared/` reúne componentes y utilidades que se reutilizan en distintas
  funcionalidades.
- `services/` contiene el acceso a servicios externos, incluida la comunicación
  con la API.

El frontend se comunica con el backend mediante HTTP y JSON; no accede
directamente a las bases de datos. Estas carpetas se irán creando cuando se
incorporen sus primeros archivos.

## Datos y evolución

PostgreSQL será la fuente operacional del sistema. ClickHouse estará separado
para atender consultas analíticas sobre el histórico. El modelo histórico y el
mecanismo para alimentar ClickHouse se definirán durante la implementación.
