# Horarios de armado manual

**Requisitos relacionados:** parte de RF14–RF16 para el registro y la composición inicial del horario. La generación automática (RF12–RF13), la programación completa de imparticiones y la validación de conflictos de aula (RF17) quedan para hitos posteriores.
**Convenciones comunes:** [Contrato de la API](../contrato-api.md).

## Alcance M4

En M4, el horario se arma manualmente. “Borrador” describe conceptualmente que su programación todavía está incompleta; no es un estado persistido. El horario registra programa, periodo académico, coordinador creador, autorización utilizada, fecha de creación y estado `GENERADO`, que significa que está constituido. No se distingue si se armó manualmente o mediante el algoritmo.

Cada asociación entre horario y bloque incluye una fecha obligatoria. En M4, hora de inicio, hora de fin, periodo de impartición y aula quedan sin completar.

## Endpoints

| Método y ruta | Resultado |
|---|---|
| `POST /api/horarios` | Crea un horario armado manualmente con bloques y sus fechas. |
| `GET /api/horarios` | Lista horarios; filtros opcionales `programaId`, `periodoAcademico`, `estado`. |
| `GET /api/horarios/{horarioId}` | Consulta datos del horario y los bloques seleccionados. |

Crear horario manual:

```json
{
  "programaId": "uuid",
  "periodoAcademico": "2026-2027",
  "coordinadorId": "uuid",
  "bloques": [
    {
      "bloqueId": "uuid",
      "fecha": "2026-10-20"
    },
    {
      "bloqueId": "uuid",
      "fecha": "2026-10-22"
    }
  ]
}
```

La API resuelve la autorización vigente del coordinador para el programa. El cliente no elige ni envía la fecha de inicio de esa autorización.

Respuesta `201 Created`:

```json
{
  "id": "uuid",
  "programaId": "uuid",
  "periodoAcademico": "2026-2027",
  "coordinadorId": "uuid",
  "fechaInicioAutorizacion": "2026-09-07",
  "fechaCreacion": "2026-10-12T14:30:00Z",
  "estado": "generado",
  "cantTotalBloques": 2,
  "bloques": [
    {
      "bloqueId": "uuid",
      "descripcion": "Introducción a las bases de datos",
      "asignaturaId": "uuid",
      "tipoActividad": "teoria",
      "nivelComplejidad": "medio",
      "fecha": "2026-10-20",
      "horaInicio": null,
      "horaFin": null,
      "periodoImp": null,
      "aulaId": null
    },
    {
      "bloqueId": "660e8400-e29b-41d4-a716-446655440000",
      "descripcion": "Consultas y operaciones SQL",
      "asignaturaId": "uuid",
      "tipoActividad": "practica",
      "nivelComplejidad": "medio",
      "fecha": "2026-10-22",
      "horaInicio": null,
      "horaFin": null,
      "periodoImp": null,
      "aulaId": null
    }
  ]
}
```

## Reglas y validaciones

- Programa y coordinador deben existir.
- `bloques` debe contener al menos un elemento. Cada elemento requiere `bloqueId` y `fecha`.
- El coordinador debe tener una autorización vigente para el programa, con `puedeGenerar = true`, al crear el horario. El horario conserva la fecha de inicio de esa autorización para la relación compuesta del modelo.
- Todos los bloques deben existir y estar asociados al programa indicado. Un mismo bloque no puede repetirse para la misma fecha dentro del horario; puede programarse en fechas diferentes.
- En M4, `horaInicio`, `horaFin`, `periodoImp` y `aulaId` quedan nulos. Cuando se complete una impartición, los cuatro campos serán obligatorios y deberá cumplirse `horaInicio < horaFin`.
- `cantTotalBloques` se calcula contando los bloques distintos asociados al horario; si un mismo bloque aparece en varias fechas, cuenta una sola vez.
- La fecha de creación la determina el servidor en UTC.
- El estado lo establece el servidor como `GENERADO`. En M4 significa que el horario está constituido; no indica si se creó manualmente o mediante el algoritmo. El cliente no envía el estado.
- La creación del horario y sus asociaciones debe ser atómica: si un bloque no es válido, no debe quedar un horario parcialmente creado.
- El `coordinadorId` identifica la atribución funcional. Sin autenticación, no verifica quién envió la solicitud.

## Relación con el esquema normalizado

M4 utiliza la relación ternaria existente `BLOQUE_EN_HORARIO`, con clave `(idHorario, idBloque, fecha)`. La fecha es obligatoria. Un mismo bloque puede aparecer en distintas fechas, pero no se puede repetir la misma combinación de horario, bloque y fecha. No se crea una relación adicional para la composición del horario.

En M4, `horaInicio`, `horaFin`, `periodoImp` e `idAula` deben estar todos sin valor. Al completar una impartición, los cuatro campos deben informarse. La persistencia debe impedir que solo algunos estén informados y debe validar `horaInicio < horaFin` cuando exista una programación completa.

El horario referencia la autorización concreta de `COORDINADOR_EN_PROGRAMA` mediante coordinador, programa y fecha de inicio. La API busca la autorización vigente con permiso `puedeGenerar = true` y conserva la fecha de inicio de esa autorización en el horario.

La validación de solapamientos de aulas (RII04) se aplicará cuando exista programación completa y corresponde a M6.

## Errores esperados

- `400`: periodo vacío, lista vacía, UUID inválido o fecha ausente.
- `404`: programa, coordinador o bloque inexistente.
- `403`: el coordinador no tiene autorización vigente o permiso de generación para el programa.
- `409`: bloque no asociado al programa, duplicación de la combinación horario-bloque-fecha o conflicto con las reglas de integridad.
