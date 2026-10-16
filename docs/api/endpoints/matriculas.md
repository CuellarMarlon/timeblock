# Matrículas

**Requisitos relacionados:** RF07, RI07, RI21 como referencia futura.  
**Convenciones comunes:** [Contrato de la API](../contrato-api.md).

M4 registra la relación de un estudiante con un programa. Las asignaciones de horarios a estudiantes pertenecen a requisitos posteriores y no se realizan mediante estos endpoints.

## Endpoints

| Método y ruta | Resultado |
|---|---|
| `POST /api/programas/{programaId}/matriculas` | Matricula un estudiante en el programa. |
| `GET /api/programas/{programaId}/matriculas` | Lista estudiantes matriculados. |
| `GET /api/estudiantes/{estudianteId}/matriculas` | Lista programas donde está matriculado el estudiante. |

Crear matrícula:

```http
POST /api/programas/{programaId}/matriculas
Content-Type: application/json
```

```json
{
  "estudianteId": "uuid"
}
```

Respuesta `201 Created`:

```json
{
  "estudianteId": "uuid",
  "programaId": "uuid"
}
```

## Validaciones

- El identificador debe corresponder a una persona con perfil de estudiante.
- El programa debe existir.
- La combinación estudiante + programa es única; una segunda matrícula responde `409 Conflict`.
- Listados usan la paginación común.

## Errores esperados

- `400`: falta `estudianteId` o no tiene formato UUID.
- `404`: estudiante, perfil o programa inexistente.
- `409`: el estudiante ya está matriculado en ese programa.

En M4 no se definen fechas de baja, estado histórico de matrícula ni operaciones de desmatriculación.
