# Programas académicos y asignaturas

**Requisitos relacionados:** RF05–RF06, RI05–RI06, RI08.  
**Convenciones comunes:** [Contrato de la API](../contrato-api.md).

## Programas académicos

| Método y ruta | Resultado |
|---|---|
| `POST /api/programas` | Crea un programa. |
| `GET /api/programas` | Lista programas; filtro opcional `nombre`. |
| `GET /api/programas/{programaId}` | Consulta un programa. |
| `PUT /api/programas/{programaId}` | Actualiza nombre y plan de estudios. |
| `GET /api/programas/{programaId}/asignaturas` | Lista las asignaturas asociadas. |

Crear programa:

```json
{
  "nombre": "Ingeniería Informática",
  "planEstudio": "Plan 2024"
}
```

Respuesta `201 Created`:

```json
{
  "id": "uuid",
  "nombre": "Ingeniería Informática",
  "planEstudio": "Plan 2024"
}
```

## Asignaturas

| Método y ruta | Resultado |
|---|---|
| `POST /api/asignaturas` | Crea una asignatura. |
| `GET /api/asignaturas` | Lista asignaturas; filtro opcional `nombre`. |
| `GET /api/asignaturas/{asignaturaId}` | Consulta una asignatura. |
| `PUT /api/asignaturas/{asignaturaId}` | Actualiza el nombre. |
| `PUT /api/programas/{programaId}/asignaturas/{asignaturaId}` | Asocia la asignatura al programa; idempotente. |

Crear asignatura:

```json
{
  "nombre": "Bases de Datos II"
}
```

Asociar asignatura:

```http
PUT /api/programas/{programaId}/asignaturas/{asignaturaId}
```

Respuesta para una asociación creada o ya existente: `204 No Content`.

## Validaciones

- Nombre y plan de estudio son obligatorios para un programa.
- El nombre de asignatura es obligatorio.
- Programa y asignatura deben existir antes de asociarlos.
- La relación programa-asignatura no puede repetirse.
- El identificador de programa y asignatura no se cambia mediante `PUT`.

## Errores esperados

- `400`: campos obligatorios vacíos o inválidos.
- `404`: programa o asignatura inexistente.
- `409`: duplicidad según restricciones del modelo de datos.

M4 no define eliminación de programas, asignaturas ni asociaciones, porque el diseño no especifica el efecto sobre matrículas, bloques u horarios ya creados.
