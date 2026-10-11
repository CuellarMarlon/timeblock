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

Si utilizas nvm para gestionar Node.js, desde la raíz del repositorio ejecuta:

```bash
nvm install
nvm use
```

Instala pnpm en la versión acordada. Con npm 11, autoriza los scripts de
instalación de pnpm para este paquete:

```bash
npm install --global --allow-scripts=pnpm pnpm@12.10.1
```

Verifica que `pnpm --version` muestre `12.10.1`.

## Preparar las bases de datos locales

Desde la raíz, crea la configuración local a partir del ejemplo:

```bash
cp .env.example .env
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

Los proyectos de pruebas xUnit están preparados, pero las pruebas de las
funcionalidades se irán añadiendo conforme se implemente el sistema.

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

Docker Compose inicializa PostgreSQL y ClickHouse para el desarrollo local.
Actualmente el backend todavía no está conectado a esas bases de datos y el
repositorio no contiene migraciones de Entity Framework Core. Por eso, no hay
un comando de migración que ejecutar todavía. Cuando se incorpore la
persistencia, se documentarán aquí los pasos para crear y aplicar migraciones.
