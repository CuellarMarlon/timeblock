# Horarios de armado manual

**Requisitos relacionados:** parte de RF14 para el registro del horario; RF16 y RF17 (M6-07, #88) para programar imparticiones completas sin solapes de aula. La generación automática (RF12–RF13) queda para M6-09 y M6-10.
**Convenciones comunes:** [Contrato de la API](../contrato-api.md).

## Alcance M4

En M4, el horario se arma manualmente. “Borrador” describe conceptualmente que su programación todavía está incompleta; no es un estado persistido. El horario registra programa, periodo académico, coordinador creador, autorización utilizada, fecha de creación y estado `GENERADO`, que significa que está constituido. No se distingue si se armó manualmente o mediante el algoritmo.

Cada asociación entre horario y bloque incluye una fecha obligatoria. Desde M6-07 puede programarse además como **impartición completa**, con hora de inicio, hora de fin y aula; el periodo de impartición es el periodo académico del horario. Si no se envían, la asociación queda sin programar, como en M4.

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
      "fecha": "2026-10-20",
      "horaInicio": "08:00:00",
      "horaFin": "10:00:00",
      "aulaId": "uuid"
    },
    {
      "bloqueId": "uuid",
      "fecha": "2026-10-22"
    }
  ]
}
```

En el ejemplo, el primer bloque queda programado como impartición completa y el segundo, sin programar.

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
      "horaInicio": "08:00:00",
      "horaFin": "10:00:00",
      "periodoImp": "2026-2027",
      "aulaId": "uuid",
      "bloqueActivo": true
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
      "aulaId": null,
      "bloqueActivo": true
    }
  ]
}
```

## Reglas y validaciones

- Programa y coordinador deben existir.
- `bloques` debe contener al menos un elemento. Cada elemento requiere `bloqueId` y `fecha`.
- El coordinador debe tener una autorización vigente para el programa, con `puedeGenerar = true`, al crear el horario. El horario conserva la fecha de inicio de esa autorización para la relación compuesta del modelo.
- Todos los bloques deben existir y estar asociados al programa indicado. Un mismo bloque no puede repetirse para la misma fecha dentro del horario; puede programarse en fechas diferentes.
- `horaInicio`, `horaFin` y `aulaId` son opcionales, pero se envían los tres o ninguno, y debe cumplirse `horaInicio < horaFin`. Las horas usan el formato `HH:mm:ss`. El cliente no envía `periodoImp`: el servidor guarda el periodo académico del horario.
- El aula debe existir.
- Una impartición ocupa el intervalo semiabierto `[horaInicio, horaFin)` de su fecha. No puede solaparse con otra impartición de la misma aula y fecha, de este u otro horario (RF17, RII04), ni con otra del mismo horario aunque use otra aula. Los intervalos contiguos, en los que una termina cuando empieza la otra, sí se permiten.
- `cantTotalBloques` se calcula contando los bloques distintos asociados al horario; si un mismo bloque aparece en varias fechas, cuenta una sola vez.
- `bloqueActivo` muestra el estado actual del bloque sin eliminarlo de horarios anteriores.
- La fecha de creación la determina el servidor en UTC.
- El estado lo establece el servidor como `GENERADO`. En M4 significa que el horario está constituido; no indica si se creó manualmente o mediante el algoritmo. El cliente no envía el estado.
- La creación del horario y sus asociaciones debe ser atómica: si un bloque no es válido, no debe quedar un horario parcialmente creado.
- El `coordinadorId` identifica la atribución funcional. Sin autenticación, no verifica quién envió la solicitud.

## Relación con el esquema normalizado

M4 utiliza la relación ternaria existente `BLOQUE_EN_HORARIO`, con clave `(idHorario, idBloque, fecha)`. La fecha es obligatoria. Un mismo bloque puede aparecer en distintas fechas, pero no se puede repetir la misma combinación de horario, bloque y fecha. No se crea una relación adicional para la composición del horario.

`horaInicio`, `horaFin`, `periodoImp` e `idAula` están todos sin valor o todos informados, con `horaInicio < horaFin` (`CK_BLOQUE_EN_HORARIO_ImparticionCompleta`).

El horario referencia la autorización concreta de `COORDINADOR_EN_PROGRAMA` mediante coordinador, programa y fecha de inicio. La API busca la autorización vigente con permiso `puedeGenerar = true` y conserva la fecha de inicio de esa autorización en el horario.

Los solapamientos se impiden también en PostgreSQL, con dos restricciones de exclusión sobre el intervalo `tsrange(fecha + horaInicio, fecha + horaFin, '[)')` (extensión `btree_gist`):

- `EX_BLOQUE_EN_HORARIO_AulaSinSolapes`: misma aula (RII04).
- `EX_BLOQUE_EN_HORARIO_HorarioSinSolapes`: mismo horario, aunque cambie el aula.

Se mantiene el índice único `UX_BLOQUE_EN_HORARIO_AulaFechaHoraInicio`, que corresponde a la clave alternativa `(idAula, fecha, horaInicio)` del diseño. Las restricciones de exclusión también actúan ante altas simultáneas: la segunda espera a la primera y se rechaza si esta confirma.

## Notas de implementación

- La API valida en este orden: formato de la solicitud (`400`), programa y coordinador (`404`), autorización del coordinador (`403`), existencia de los bloques y de las aulas (`404`) y reglas de composición y solapamiento (`409`). Si hay varios problemas, se informa el primero.
- La autorización se resuelve con la fecha actual del servidor (UTC): se usa una vigente con `puedeGenerar = true` y, si hay varias, la de inicio más reciente. Una que vence hoy sigue vigente hoy.
- `GET /api/horarios` usa la paginación común y devuelve cada horario con sus bloques, igual que la consulta individual, del más reciente al más antiguo. El filtro `periodoAcademico` compara el texto exacto, sin espacios en los extremos.
- Dentro de un horario, los bloques se devuelven ordenados por fecha y descripción.
- Se ignoran las propiedades que el cliente envíe y no formen parte de la solicitud, como `estado` o `fechaCreacion`.
- El horario y sus asociaciones se guardan en una sola operación de base de datos; las fechas que aún no existan en `FECHA` se crean en esa misma operación.

## Errores esperados

- `400`: periodo vacío, lista vacía, UUID inválido, fecha ausente, programación incompleta (`horaInicio`, `horaFin` y `aulaId` no van juntos), hora mal formada o `horaInicio` no anterior a `horaFin`.
- `404`: programa, coordinador, bloque o aula inexistente.
- `403`: el coordinador no tiene autorización vigente o permiso de generación para el programa.
- `409`: bloque no asociado al programa, duplicación de la combinación horario-bloque-fecha, impartición solapada con otra del mismo horario, aula ocupada en ese intervalo o conflicto con las reglas de integridad.
