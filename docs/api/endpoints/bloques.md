# Bloques de contenido

**Requisitos relacionados:** RF08–RF11, RF15, RI09–RI11, RII03.  
**Convenciones comunes:** [Contrato de la API](../contrato-api.md).

Un bloque contiene una descripción, una asignatura, el coordinador creador, un tipo de actividad, una complejidad y cero o más programas donde puede utilizarse.

## Endpoints

| Método y ruta | Resultado |
|---|---|
| `POST /api/bloques` | Crea un bloque con sus asociaciones iniciales a programas. |
| `GET /api/bloques` | Lista bloques; filtros opcionales `programaId`, `asignaturaId`, `coordinadorId`, `tipoActividad`, `nivelComplejidad`. |
| `GET /api/bloques/{bloqueId}` | Consulta bloque y programas asociados. |
| `PUT /api/bloques/{bloqueId}` | Actualiza descripción, actividad, complejidad o asignatura. |
| `PUT /api/bloques/{bloqueId}/programas/{programaId}` | Asocia el bloque con un programa; idempotente. |

Crear bloque:

```json
{
  "descripcion": "Introducción a las bases de datos",
  "tipoActividad": "teoria",
  "nivelComplejidad": "medio",
  "asignaturaId": "uuid",
  "coordinadorId": "uuid",
  "programasIds": ["uuid"]
}
```

Respuesta `201 Created`:

```json
{
  "id": "uuid",
  "descripcion": "Introducción a las bases de datos",
  "tipoActividad": "teoria",
  "nivelComplejidad": "medio",
  "asignaturaId": "uuid",
  "coordinadorId": "uuid",
  "programasIds": ["uuid"]
}
```

## Reglas y validaciones

- `tipoActividad`: `teoria`, `practica` o `laboratorio`.
- `nivelComplejidad`: `bajo`, `medio` o `alto`.
- Descripción, asignatura, coordinador y tipo de actividad son obligatorios.
- La asignatura y el coordinador deben existir; el coordinador debe tener el perfil correspondiente.
- Para asociar un bloque a un programa, la asignatura del bloque debe pertenecer al programa (RII03).
- El creador se guarda para atribución funcional. Como M4 no implementa autenticación, el `coordinadorId` recibido no autentica a quien envía la solicitud.
- Bloques creados por otro coordinador se pueden utilizar si están asociados al programa (RF15).

## Duplicidad técnica

La clave lógica de duplicidad es asignatura + tipo de actividad + descripción normalizada. La normalización acordada para M4:

1. Quitar espacios iniciales y finales.
2. Reemplazar cada secuencia de espacios en blanco por un espacio.
3. Comparar sin distinguir mayúsculas y minúsculas.
4. Conservar los acentos.

La normalización y la restricción de unicidad deben aplicarse en el dominio/persistencia, no solo en el formulario. La detección semántica de descripciones distintas con contenido parecido queda fuera de M4.

## Errores esperados

- `400`: enum inválido, descripción vacía o asociación incompatible.
- `404`: asignatura, coordinador, programa o bloque inexistente.
- `409`: bloque duplicado o relación duplicada/conflictiva.
