# Horarios borrador

**Requisitos relacionados:** parte de RF14–RF15 para el registro y la composición inicial del horario. La generación automática (RF12–RF13) y las imparticiones (RF16–RF17) quedan fuera de M4.  
**Convenciones comunes:** [Contrato de la API](../contrato-api.md).

## Alcance M4

Un horario M4 es un borrador que registra programa, periodo académico, coordinador creador, fecha de creación, estado y bloques seleccionados. No contiene todavía fechas de impartición, horas, aulas ni parámetros de un algoritmo generador.

## Endpoints

| Método y ruta | Resultado |
|---|---|
| `POST /api/horarios` | Crea un horario borrador con los bloques seleccionados. |
| `GET /api/horarios` | Lista horarios; filtros opcionales `programaId`, `periodoAcademico`, `estado`. |
| `GET /api/horarios/{horarioId}` | Consulta datos del horario y los bloques seleccionados. |

Crear borrador:

```json
{
  "programaId": "uuid",
  "periodoAcademico": "2026-2027",
  "coordinadorId": "uuid",
  "bloquesIds": ["uuid", "uuid"]
}
```

Respuesta `201 Created`:

```json
{
  "id": "uuid",
  "programaId": "uuid",
  "periodoAcademico": "2026-2027",
  "coordinadorId": "uuid",
  "fechaCreacion": "2026-10-12T14:30:00Z",
  "estado": "borrador",
  "bloques": [
    {
      "id": "uuid",
      "descripcion": "Introducción a las bases de datos",
      "asignaturaId": "uuid",
      "tipoActividad": "teoria",
      "nivelComplejidad": "medio"
    }
  ]
}
```

## Reglas y validaciones

- Programa y coordinador deben existir.
- `bloquesIds` debe contener al menos un UUID.
- El coordinador debe tener una autorización vigente para el programa, con `puedeGenerar = true`, al momento de crear el borrador.
- Todos los bloques deben existir, no repetirse en la solicitud y estar asociados al programa indicado.
- La fecha de creación la determina el servidor en UTC.
- El estado inicial lo determina el servidor y es `borrador`; el cliente no puede enviar un estado distinto.
- La creación del horario y de su composición debe ser atómica: si un bloque no es válido, no debe quedar un horario parcialmente creado.
- El `coordinadorId` identifica la atribución funcional. Sin autenticación, no verifica quién envió la solicitud.

## Decisión de modelo pendiente de integrar

En el diseño de base de datos, `BLOQUE_EN_HORARIO` representa una impartición y requiere fecha, hora de inicio, hora de fin y aula. Por tanto, no puede representar la selección de bloques de un borrador M4 sin inventar valores de impartición.

La persistencia M4 debe representar por separado la composición/selección del borrador, por ejemplo mediante una asociación `HORARIO_BLOQUE_SELECCIONADO`. La issue de modelo e integración debe confirmar el nombre y sus claves antes de crear la migración. Cuando M6 implemente las sesiones reales, estas usarán la entidad de impartición con fecha, horario, intervalo y aula.

## Errores esperados

- `400`: periodo vacío, lista vacía, UUID inválido o IDs duplicados en la solicitud.
- `404`: programa, coordinador o bloque inexistente.
- `403`: el coordinador no tiene autorización vigente o permiso de generación para el programa.
- `409`: bloque no asociado al programa o conflicto con las reglas de integridad.
