# Personas, perfiles y autorizaciones

**Requisitos relacionados:** RF01–RF04, RI01–RI04.  
**Convenciones comunes:** [Contrato de la API](../contrato-api.md).

## Crear una persona con un perfil

`POST /api/personas`

El perfil se crea junto con la persona para evitar personas sin perfil o personas con perfiles múltiples. El `tipo` determina los campos de `perfil`.

### Estudiante

```json
{
  "nombre": "Ana Pérez",
  "perfil": {
    "tipo": "estudiante",
    "edad": 20,
    "grupo": "C311",
    "coordinadorId": "uuid"
  }
}
```

### Coordinador

```json
{
  "nombre": "Luis García",
  "perfil": {
    "tipo": "coordinador",
    "especialidad": "Bases de datos"
  }
}
```

### Jefe de departamento

```json
{
  "nombre": "María Rodríguez",
  "perfil": {
    "tipo": "jefeDepartamento"
  }
}
```

Respuesta `201 Created`:

```json
{
  "id": "uuid",
  "nombre": "Ana Pérez",
  "perfil": {
    "tipo": "estudiante",
    "edad": 20,
    "grupo": "C311",
    "coordinadorId": "uuid"
  }
}
```

## Consultar y actualizar personas

| Método y ruta | Resultado |
|---|---|
| `GET /api/personas` | Lista paginada; filtro opcional `nombre` y `tipoPerfil`. |
| `GET /api/personas/{personaId}` | Persona y su único perfil. |
| `PUT /api/personas/{personaId}` | Actualiza los datos comunes de la persona. |
| `PUT /api/personas/{personaId}/perfil` | Actualiza los campos del perfil existente; no cambia su tipo. |

El `PUT` de perfil requiere el mismo tipo que ya tiene la persona. Ejemplo de actualización de perfil de estudiante:

```json
{
  "tipo": "estudiante",
  "edad": 21,
  "grupo": "C312",
  "coordinadorId": "uuid"
}
```

Para coordinador, el cuerpo usa `{"tipo":"coordinador","especialidad":"..."}`; para jefe de departamento, `{"tipo":"jefeDepartamento"}`.

El identificador y el tipo de perfil no se cambian con `PUT`. Una conversión de perfil requiere una regla explícita y queda fuera del contrato M4 inicial.

## Autorizaciones de coordinador por programa

| Método y ruta | Resultado |
|---|---|
| `POST /api/coordinadores/{coordinadorId}/autorizaciones` | Crea una autorización. |
| `GET /api/coordinadores/{coordinadorId}/autorizaciones` | Lista autorizaciones; filtros opcionales por `programaId` y vigencia. |
| `PUT /api/coordinadores/{coordinadorId}/autorizaciones/{programaId}/{fechaInicio}` | Actualiza fin de vigencia y permisos. |

Request:

```json
{
  "programaId": "uuid",
  "fechaInicio": "2026-10-12",
  "fechaFin": null,
  "puedeGenerar": true,
  "puedeValidar": false
}
```

La clave lógica es coordinador + programa + fecha de inicio. `fechaFin` puede ser nula; si existe, no puede ser anterior a `fechaInicio`.

## Validaciones

- `nombre`, `grupo`, `especialidad` y los campos obligatorios del perfil no pueden estar vacíos.
- La edad debe ser mayor que cero.
- `coordinadorId` debe referir a una persona con perfil de coordinador.
- El programa de una autorización debe existir.
- Una persona solo puede tener un perfil.
- Si el estudiante requiere coordinador al crearse, se debe crear primero el coordinador.

## Errores esperados

- `400`: perfil desconocido, datos incompletos, edad no positiva o fechas inválidas.
- `404`: persona, coordinador o programa inexistente.
- `409`: persona ya registrada o perfil ya existente para esa persona.
