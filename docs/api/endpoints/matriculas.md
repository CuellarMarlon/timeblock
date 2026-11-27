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
| `DELETE /api/programas/{programaId}/matriculas/{estudianteId}` | Desmatricula al estudiante del programa. |

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
- `204`: matrícula eliminada correctamente; no se devuelve cuerpo.
- `404`: la matrícula indicada no existe.

En M4 se puede desmatricular eliminando la relación entre estudiante y programa.
La operación no elimina al estudiante ni al programa y permite volver a matricular
a la misma pareja. No se conservan fechas ni historial de bajas de matrícula.