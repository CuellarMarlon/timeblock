# Timeblock

Sistema web para la gestión, generación y análisis de horarios docentes.

## Estado del proyecto

El repositorio contiene la estructura inicial del backend y el frontend, además
del entorno local de PostgreSQL y ClickHouse. La conexión del backend con
PostgreSQL, las migraciones y las funcionalidades del sistema se implementarán
en las siguientes etapas. ClickHouse todavía no recibe datos de la aplicación.

## Tecnologías

- Backend: .NET 10 y ASP.NET Core.
- Frontend: React, Vite y TypeScript.
- Node.js: 24.21.0 (`.nvmrc`).
- Gestor de paquetes frontend: pnpm 12.10.1.
- Base operacional: PostgreSQL 18.
- Almacén analítico: ClickHouse.
- Servicios de datos locales: Docker Compose.

La arquitectura y las responsabilidades de cada componente están descritas en
[`docs/arquitectura.md`](docs/arquitectura.md).

## Requisitos

Instala las siguientes herramientas:

- .NET SDK 10.
- Node.js 24.21.0.
- pnpm 12.10.1.
- Docker Engine y Docker Compose v2.
- Git.

El proyecto puede desarrollarse en Linux, macOS o Windows. En Windows se
recomienda usar PowerShell y Docker Desktop con el backend WSL 2. Los comandos
de .NET, pnpm y Docker Compose son los mismos en los tres sistemas; donde cambia
la instrucción se indican las alternativas.

Comprueba las versiones con:

```bash
dotnet --version
node --version
pnpm --version
docker --version
docker compose version
```

## Obtener el proyecto

```bash
git clone https://github.com/CuellarMarlon/timeblock.git
cd timeblock
```

Instala Node.js 24.21.0. Si usas nvm en Linux o macOS, desde la raíz ejecuta:

```bash
nvm install
nvm use
```

En Windows, con nvm-windows, ejecuta en PowerShell:

```powershell
nvm install 24.21.0
nvm use 24.21.0
```

También puedes instalar esa versión directamente desde el sitio oficial de
Node.js o usar otro gestor de versiones compatible con tu sistema.

Instala pnpm en la versión acordada. Con npm 11, autoriza los scripts de
instalación de pnpm para este paquete:

```bash
npm install --global --allow-scripts=pnpm pnpm@12.10.1
```

Verifica que `pnpm --version` muestre `12.10.1`.

## Preparar las bases de datos locales

Desde la raíz, crea el archivo local de configuración a partir del ejemplo.
En Linux o macOS:

```bash
cp .env.example .env
```

En Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

Puedes ajustar los valores de `.env` para tu computadora. Ese archivo es local y
no debe subirse al repositorio; `.env.example` contiene valores de desarrollo
para que cada integrante pueda preparar su propio entorno.

Valida la configuración y arranca los servicios:

```bash
docker compose config --quiet
docker compose up -d
docker compose ps
```

PostgreSQL queda disponible en `localhost:5432`. ClickHouse expone HTTP en
`localhost:8123` y su protocolo nativo en `localhost:9000`. Los datos se
conservan en volúmenes administrados por Docker.

Para detener los servicios sin borrar los datos:

```bash
docker compose down
```

Para borrar también los datos persistidos, usa `docker compose down -v`. Esta
operación es destructiva y elimina los volúmenes locales de ambas bases de datos.

## Ejecutar el backend

Desde la raíz del repositorio:

```bash
dotnet restore backend/Timeblock.sln
dotnet build backend/Timeblock.sln
dotnet test backend/Timeblock.sln
```

Para iniciar la API con el perfil HTTP de desarrollo:

```bash
dotnet run --project backend/src/Timeblock.Api/Timeblock.Api.csproj --launch-profile http
```

La API escucha en `http://localhost:5131`. En desarrollo, el documento OpenAPI
está disponible en `http://localhost:5131/openapi/v1.json`.

### Acceso del frontend (CORS)

En desarrollo, la API acepta solicitudes desde los orígenes indicados en
`Cors:AllowedOrigins` de `appsettings.Development.json` (por defecto,
`http://localhost:5173` y `http://127.0.0.1:5173`, el puerto de Vite). Si el
frontend usa otro origen, agregarlo a esa lista. Fuera de desarrollo no se
autoriza ningún origen externo hasta configurarlo, por ejemplo con la variable
de entorno `Cors__AllowedOrigins__0`.

REVISAR ESTO Con el perfil `https`, la API redirige el tráfico HTTP a HTTPS, lo que impide
las solicitudes CORS al puerto HTTP. Para el frontend local usa el perfil
`http` o la URL `https://localhost:7161`.

### Errores y paginación

Los errores se devuelven como Problem Details (`application/problem+json`) y los
listados usan `page`, `pageSize`, `items` y `totalItems`, según el
[contrato de la API](docs/api/contrato-api.md). Los casos de uso y las
convenciones de Application se describen en
[`docs/arquitectura.md`](docs/arquitectura.md).

### Pruebas del backend

`dotnet test backend/Timeblock.sln` ejecuta las pruebas de Domain, Application,
Api e Infrastructure. Las de Api levantan la aplicación en memoria y no
necesitan PostgreSQL. Las pruebas de persistencia de Infrastructure se omiten si
no defines `TIMEBLOCK_TEST_CONNECTION` (ver más abajo).

## Ejecutar el frontend

Abre otra terminal y, desde la raíz del repositorio, ejecuta:

```bash
cd frontend
pnpm install --frozen-lockfile
pnpm run dev
```

Vite mostrará en la terminal la dirección local de la aplicación, normalmente
`http://localhost:5173`.

## Verificaciones del frontend

Ejecuta estos comandos desde `frontend/`:

```bash
pnpm run lint
pnpm run format:check
pnpm run test:run
pnpm run build
```

Para formatear los archivos reconocidos por Prettier:

```bash
pnpm run format
```

## Base de datos y migraciones

Docker Compose inicializa PostgreSQL y ClickHouse para el desarrollo local. La
persistencia transaccional M4 usa PostgreSQL mediante EF Core y Npgsql.

### Cadena de conexión local

La API no carga automáticamente el archivo `.env` de Docker Compose. Configura
la cadena de conexión mediante User Secrets, usando los valores locales de tu
`.env`:

```bash
dotnet user-secrets init --project backend/src/Timeblock.Api/Timeblock.Api.csproj
dotnet user-secrets set "ConnectionStrings:Timeblock" "Host=localhost;Port=5432;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>" --project backend/src/Timeblock.Api/Timeblock.Api.csproj
```

Reemplaza los valores entre `<...>` localmente. No publiques la cadena real ni
la guardes en archivos versionados.

### Migraciones

Restaura la herramienta EF Core definida para el repositorio y verifica el
estado de las migraciones:

```bash
dotnet tool restore
dotnet ef migrations list --project backend/src/Timeblock.Infrastructure/Timeblock.Infrastructure.csproj --startup-project backend/src/Timeblock.Api/Timeblock.Api.csproj
```

Para aplicar la migración M4 a la base configurada:

```bash
dotnet ef database update --project backend/src/Timeblock.Infrastructure/Timeblock.Infrastructure.csproj --startup-project backend/src/Timeblock.Api/Timeblock.Api.csproj
```

### Decisiones de esquema M4

- `ESQUEMA_LLAVE` (R16) se crea físicamente con sus nueve columnas obligatorias
  y su clave primaria compuesta para cumplir la PLJ. No es un agregado de Domain.
- Las claves foráneas de R16 se establecen hacia las relaciones incluidas en M4;
  las referencias a relaciones de hitos posteriores quedan fuera del esquema M4.
- RII03 se protege en PostgreSQL con triggers que impiden asociar un bloque a
  un programa que no incluya su asignatura, y que impiden invalidar una
  asociación existente al cambiar sus relaciones.
- `cantTotalBloques` no se duplica como columna persistida: se deriva contando
  bloques distintos asociados al horario, independientemente de las fechas.
- En `BLOQUE_EN_HORARIO`, la fecha es obligatoria; hora, periodo de impartición
  y aula son nulos en M4, o se informan juntos con horas válidas cuando exista
  una impartición completa.

### Pruebas PostgreSQL de Infrastructure

La prueba de integración `PostgreSqlPersistenceTests` usa una base de pruebas
ya migrada. Proporciona la cadena por la variable `TIMEBLOCK_TEST_CONNECTION`
al ejecutar `dotnet test`; la prueba guarda y recupera datos dentro de una
transacción que termina con rollback.
