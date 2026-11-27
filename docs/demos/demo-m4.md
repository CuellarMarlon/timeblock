# Demostración del recorrido funcional de M4

Guion reproducible preparado en el [PR #75](https://github.com/CuellarMarlon/timeblock/pull/75): muestra el núcleo implementado en M4 desde la interfaz hasta PostgreSQL, sus rechazos y lo que queda pendiente. El recorrido es el mismo que verificó #40. El PR lo relaciona con #70, cuya issue no se pudo consultar al revisar esta documentación.

**Estado del documento:** guion y ensayo de la versión indicada, no demostración de M6. La issue #66 y sus bajas se integraron después; los acuerdos de #67 y #71 todavía requieren implementación. En M6 se acordó retirar el alta manual de horarios y eliminar los horarios de prueba de M4, por lo que este guion y su script deberán adaptarse antes de usarse con ese alcance. La presentación y sus observaciones siguen sin constar en la sección 6.

| Dato                  | Valor                                               |
| --------------------- | --------------------------------------------------- |
| Versión               | `develop` en `e847c99` más el script de este cambio |
| Datos                 | `scripts/demo/cargar-datos-demo.mjs`                |
| Duración aproximada   | 15 minutos                                          |
| Fecha de presentación | Pendiente (sección 6)                               |

## 1. Preparar el entorno

Requisitos y configuración, como en el README: `.env`, `docker compose up -d`, cadena de conexión en User Secrets y migraciones aplicadas con `dotnet ef database update`.

1. Arrancar la API con el perfil `http`:

   ```bash
   dotnet run --project backend/src/Timeblock.Api/Timeblock.Api.csproj --launch-profile http
   ```

2. Arrancar el frontend en otra terminal:

   ```bash
   cd frontend
   pnpm run dev
   ```

3. Cargar los datos de la demostración desde la raíz del repositorio:

   ```bash
   node scripts/demo/cargar-datos-demo.mjs
   ```

   El script crea los datos **a través de la API**, así que recorre las mismas validaciones que la interfaz. Se puede repetir: lo que ya existe se reutiliza. Al final comprueba siete rechazos y termina con código 1 si alguno no da el código esperado. Con `--api <url>` usa otra dirección y con `--sin-rechazos` solo carga datos.

Para empezar con una base vacía sin perder la de desarrollo, se puede crear otra base en el mismo PostgreSQL, aplicarle las migraciones y arrancar la API con `ConnectionStrings__Timeblock` apuntando a ella.

## 2. Datos que se cargan

| Entidad        | Datos                                                                                                                                                                                                       |
| -------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Personas       | 1 jefa de departamento, 2 coordinadores (Luis García Pérez y Elena Martín Suárez) y 6 estudiantes con su coordinador asignado.                                                                              |
| Programas      | Ingeniería Informática (Bases de Datos II, Estadística, Optimización) y Ciencia de la Computación (Bases de Datos II, Programación Declarativa, Estadística).                                               |
| Autorizaciones | Luis genera y valida en Informática y solo valida en Computación. Elena genera en Computación hasta el 2027-07-31.                                                                                          |
| Matrículas     | 7; Daniela Torres Vega está en los dos programas.                                                                                                                                                           |
| Bloques        | 8 con los tres tipos de actividad y los tres niveles. "Modelo relacional y álgebra relacional", "Dependencias funcionales y normalización" y "Estadística descriptiva" se comparten entre programas (RF15). |
| Horarios       | Uno por programa en `2026-2027`: Informática con 6 bloques (Luis) y Computación con 4 (Elena), con fechas de diciembre de 2026.                                                                             |

## 3. Recorrido

| Paso | Pantalla                | Qué mostrar                                                                                                                                                        | Requisitos             |
| ---- | ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------- |
| 1    | Personas y perfiles     | Los tres perfiles. Crear en vivo un estudiante nuevo eligiendo su coordinador. Intentar edad 0 y ver el error.                                                     | RF01–RF03              |
| 2    | Personas y perfiles     | Botón "Autorizaciones" de Luis: dos programas con permisos distintos y vigencia.                                                                                   | RF04                   |
| 3    | Programas y asignaturas | Los dos programas con sus planes. En la pestaña "Asignaturas", asociar una asignatura a un programa en vivo.                                                       | RF05, RF06             |
| 4    | Matrícula               | Matricular en vivo al estudiante del paso 1 buscándolo por nombre (#65). Repetir la matrícula y ver el rechazo. Filtrar por estudiante y por programa.             | RF07                   |
| 5    | Bloques de contenido    | Filtrar por programa y ver los bloques compartidos. Crear un bloque con la descripción de uno existente cambiando espacios y mayúsculas: se rechaza por duplicado. | RF08–RF11, RF15, RII03 |
| 6    | Horarios                | Los dos horarios en `GENERADO`. Abrir uno y ver sus bloques con fecha y sin hora, período ni aula. Crear un horario en vivo con Elena en Computación.              | RF14 (parcial), RII01  |
| 7    | Horarios                | Intentar crear un horario con Luis en Computación: se rechaza porque solo puede validar (403).                                                                     | RII01                  |
| 8    | PostgreSQL              | Comprobar en la base lo creado en vivo con las consultas de la sección 4.                                                                                          | —                      |

### Rechazos que comprueba el script

| Caso                                                     | Código | Regla                        |
| -------------------------------------------------------- | ------ | ---------------------------- |
| Estudiante con edad 0                                    | 400    | Edad positiva                |
| Estudiante cuyo coordinador es un jefe de departamento   | 404    | Coordinador con su perfil    |
| Matrícula repetida                                       | 409    | Clave de MATRICULA           |
| Bloque duplicado con otros espacios y mayúsculas         | 409    | RF11, duplicidad normalizada |
| Bloque asociado a un programa que no tiene su asignatura | 400    | RII03                        |
| Horario de un coordinador sin permiso de generación      | 403    | RII01                        |
| Horario con un bloque no asociado al programa            | 409    | RF15                         |

## 4. Comprobación en PostgreSQL

Con el entorno de Docker Compose (sustituir usuario y base por los de `.env`):

```bash
docker compose exec postgres psql -U timeblock_dev -d timeblock
```

```sql
-- Personas por perfil.
SELECT 'estudiante' AS perfil, count(*) FROM "ESTUDIANTE"
UNION ALL SELECT 'coordinador', count(*) FROM "COORDINADOR"
UNION ALL SELECT 'jefe', count(*) FROM "JEFE_DEPTO";

-- Horarios con su estado, autorización usada y número de bloques distintos.
SELECT h."idHorario", p."nombrePrograma", h."periodoAcademico", h."estadoHorario",
       h."fechaInicioGen", count(DISTINCT b."idBloque") AS bloques
FROM "HORARIO" h
JOIN "PROGRAMA_ACADEMICO" p ON p."idPrograma" = h."idPrograma"
JOIN "BLOQUE_EN_HORARIO" b ON b."idHorario" = h."idHorario"
GROUP BY h."idHorario", p."nombrePrograma";

-- En M4 las imparticiones no tienen hora, período ni aula.
SELECT count(*) AS total,
       count(*) FILTER (WHERE "horaInicio" IS NULL AND "idAula" IS NULL) AS sin_programar
FROM "BLOQUE_EN_HORARIO";
```

## 5. Limitaciones y pendientes que se presentan

Según `docs/revisiones/cobertura-diseno-m5.md`:

- **Resuelto en M5:** los selectores de matrícula ya buscan más allá de los primeros 100 registros (#65).
- **Bajas y eliminación:** no hay operaciones de baja ni de borrado (#66, en curso).
- **Identidad y permisos:** no hay usuarios; el coordinador se elige en el formulario y no se autentica (#67).
- **Programación de imparticiones:** hora, período y aula quedan nulos y no se detectan solapamientos de aula (D4, D9).
- **Fuera de M4:** generación automática, revisión y validación, asignación a estudiantes, asistencia, solicitudes de cambio, reportes y ClickHouse. Las reglas propuestas para las tres primeras están en `docs/reglas/reglas-negocio-m6.md` (#71).
- **Riesgos sin reproducir:** posibles carreras en RII01 y en el alta de fechas (D8, D10).

## 6. Registro de la presentación

Completar después de presentar.

| Dato              | Valor     |
| ----------------- | --------- |
| Fecha             | Pendiente |
| Asistentes        | Pendiente |
| Versión mostrada  | Pendiente |
| Resultado general | Pendiente |

### Observaciones recibidas

| #   | Observación | Quién | Acuerdo o issue |
| --- | ----------- | ----- | --------------- |
| 1   |             |       |                 |

### Defectos detectados

| #   | Defecto | Paso | Issue |
| --- | ------- | ---- | ----- |
| 1   |         |      |       |

## 7. Ensayo previo

Ensayo del 2026-11-25 sobre `develop` en `e847c99`, con una base vacía migrada en PostgreSQL 16:

- Primera ejecución del script: 35 registros creados y los 7 rechazos con el código esperado.
- Segunda ejecución: 0 creados y 35 ya existentes; los rechazos se repiten igual.
- En la base: 9 personas, 6 estudiantes, 7 matrículas, 8 bloques, 11 asociaciones bloque-programa y 2 horarios en `GENERADO` con 10 imparticiones, todas sin hora ni aula.
- La interfaz muestra las matrículas con sus nombres y los dos horarios con 6 y 4 bloques.
