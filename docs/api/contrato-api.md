# Contrato común de la API

Este documento contiene las convenciones que comparten los grupos de endpoints; cada módulo tiene su propio documento en `endpoints/`.

**Estado:** borrador para revisión conjunta de backend y frontend. Las decisiones de IDs, perfiles, normalización de bloques, autorización para horarios y autenticación ya fueron acordadas. Las convenciones HTTP, paginación y formas exactas de los DTO deben mantenerse sincronizadas con OpenAPI durante la implementación.

## Convenciones

- Prefijo: `/api`.
- Transporte: HTTP con cuerpos y respuestas JSON.
- Propiedades JSON: `camelCase`.
- Identificadores: UUID, generados por el servidor al crear recursos.
- Fechas sin hora: `YYYY-MM-DD`.
- Marcas de tiempo: ISO 8601 en UTC, por ejemplo `2026-10-12T14:30:00Z`.
- En los listados se propone usar `page` (por defecto `1`) y `pageSize` (por defecto `20`, máximo `100`).
- Las operaciones de creación responden `201 Created`, con el recurso creado y su URI en `Location`.
- Consultas y actualizaciones correctas responden `200 OK`; las asociaciones `PUT` idempotentes pueden responder `204 No Content`.
- No se definen operaciones `DELETE` de recursos principales; las reglas de baja y conservación se decidirán antes de necesitarlas.

## Errores

Las respuestas de error usan Problem Details (`application/problem+json`). Los errores de validación pueden incluir el objeto `errors` por campo.

```json
{
  "type": "about:blank",
  "title": "La solicitud contiene datos inválidos",
  "status": 400,
  "detail": "Revisa los campos indicados.",
  "errors": {
    "edad": ["La edad debe ser mayor que cero."]
  }
}
```

| Código | Uso |
|---:|---|
| `200` | Consulta o actualización correcta. |
| `201` | Recurso creado. |
| `204` | Asociación idempotente completada sin cuerpo. |
| `400` | JSON, parámetros o datos inválidos. |
| `403` | La autorización de negocio del coordinador no permite la operación; no representa autenticación de usuario. |
| `404` | El recurso indicado no existe. |
| `409` | Duplicado o conflicto con una regla de integridad. |
| `500` | Error inesperado; no incluir detalles internos ni secretos. |

## Respuestas de listados

Propuesta inicial pendiente de ratificación: `page` por defecto `1`; `pageSize` por defecto `20`, máximo `100`.

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 0
}
```

Los filtros disponibles se describen en el documento de cada módulo.

## Perfiles

Una persona tiene exactamente un perfil: estudiante, coordinador o jefe de departamento. El perfil se envía como una unión discriminada por `tipo`.

## Valores de enumeraciones

Los valores se envían como cadenas sin tildes:

- `tipoActividad`: `teoria`, `practica`, `laboratorio`.
- `nivelComplejidad`: `bajo`, `medio`, `alto`.
- `tipoPerfil`: `estudiante`, `coordinador`, `jefeDepartamento`.
- `estado` inicial de horario M4: `borrador`.

## Alcance M4

Incluye personas y perfiles, autorizaciones de coordinadores por programa, programas, asignaturas, matrícula, bloques de contenido y horarios borrador.

No incluye generación automática, imparticiones con fecha/hora/aula, revisión y validación completa, asignación de horarios a estudiantes, asistencia, solicitudes de cambio, reportes ni consultas analíticas en ClickHouse. Estos puntos pertenecen a hitos posteriores.

## Documentos por grupo

- [Personas, perfiles y autorizaciones](endpoints/personas-y-perfiles.md)
- [Programas y asignaturas](endpoints/programas-y-asignaturas.md)
- [Matrículas](endpoints/matriculas.md)
- [Bloques de contenido](endpoints/bloques.md)
- [Horarios borrador](endpoints/horarios.md)

## Decisiones de modelo que afectan al contrato

1. Los UUID se generan en el servidor.
2. Los perfiles de persona son excluyentes.
3. La duplicidad técnica de bloques se define por asignatura, tipo de actividad y descripción normalizada: quitar espacios al inicio/final, colapsar espacios consecutivos e ignorar mayúsculas; los acentos se conservan. Detectar que dos descripciones distintas representan el mismo contenido se deja para una etapa posterior.
4. Para crear un horario borrador se exige una autorización vigente del coordinador para el programa, con `puedeGenerar = true`.
5. El contrato de borradores distingue selección de bloques de imparticiones. `BLOQUE_EN_HORARIO` en el diseño representa una impartición y requiere fecha, hora y aula; por tanto, no se usará para guardar una mera selección de bloques de M4. El modelo relacional debe representar esa selección por separado.

Las convenciones de rutas, paginación y códigos de respuesta se deben mantener sincronizadas con el documento OpenAPI cuando se implementen los endpoints.
