# Aulas

**Requisitos relacionados:** RI16 (§7.2.13 del diseño de la base de datos).  
**Tarea:** M6-06, issue #87.  
**Convenciones comunes:** [Contrato de la API](../contrato-api.md).  
**Estado:** API y pantalla implementadas (`AulasController`, `AulasService`, `AulaRepository` en el backend; listado, alta y edición en el frontend), con los topes propuestos de 100 caracteres y capacidad 1000. Quedan pendientes los permisos reales (#84) y las decisiones abiertas al final, en [Decisiones pendientes](#decisiones-pendientes). Mientras #84 no exista, la pantalla oculta las acciones de gestión a los roles sin permiso, pero el backend no aplica autorización; ese control no sustituye al del servidor.

Un aula se describe con el edificio donde está y su capacidad. La tabla `AULA` ya existe en la base de datos (`idAula`, `edificio`, `capacidad`, con la restricción `CK_AULA_CapacidadPositiva`); esta tarea añade la API y la pantalla para registrarlas, editarlas y consultarlas.

## Endpoints

| Método y ruta | Resultado |
|---|---|
| `POST /api/aulas` | Crea un aula. |
| `GET /api/aulas` | Lista aulas con paginación; filtro opcional `edificio`. |
| `GET /api/aulas/{aulaId}` | Consulta un aula. |
| `PUT /api/aulas/{aulaId}` | Actualiza el edificio y la capacidad. |

No hay `DELETE` ni cambio de estado en esta tarea.

Crear aula:

```json
{
  "edificio": "Edificio A",
  "capacidad": 30
}
```

Respuesta `201 Created`, con la URI del aula en `Location`:

```json
{
  "id": "uuid",
  "edificio": "Edificio A",
  "capacidad": 30
}
```

Actualizar aula (`PUT /api/aulas/{aulaId}`) recibe el mismo cuerpo que crear, con los dos campos obligatorios, y responde `200 OK` con el aula actualizada.

Consultar aula (`GET /api/aulas/{aulaId}`) responde `200 OK` con el mismo objeto.

## Listado

`GET /api/aulas?edificio=Edificio&page=1&pageSize=20`

- `edificio` (opcional): texto que debe estar contenido en el nombre del edificio, sin distinguir mayúsculas.
- `page` y `pageSize`: según el [contrato común](../contrato-api.md#respuestas-de-listados).
- Orden fijo: `edificio`, luego `capacidad` y, para desempatar, `id`. Así la paginación es estable.

```json
{
  "items": [
    { "id": "uuid", "edificio": "Edificio A", "capacidad": 30 },
    { "id": "uuid", "edificio": "Edificio A", "capacidad": 45 }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 2
}
```

## Validaciones

- `edificio` es obligatorio. Se recortan los espacios al inicio y al final, y no puede quedar vacío. Longitud máxima propuesta: 100 caracteres.
- `capacidad` es obligatoria y debe ser un número entero JSON mayor que cero (no se aceptan decimales, ni números escritos como texto). Valor máximo propuesto: 1000.
- El `id` se genera en el servidor y no cambia con `PUT`.
- Dos aulas pueden tener el mismo edificio y la misma capacidad: no hay unicidad que validar (ver decisión 1).
- No existe tipo de aula. La API no lo recibe ni lo devuelve, y las aulas no se clasifican (laboratorio, teoría, etc.).
- La capacidad se guarda y se devuelve, pero no limita cuántos estudiantes se asignan a un horario ([reglas de negocio](../../reglas/reglas-negocio-m6.md)).

## Errores esperados

- `400`: JSON mal formado, `edificio` o `capacidad` ausentes o inválidos, parámetros de paginación fuera de rango, o UUID vacío. Los errores de campo van en `errors`:

```json
{
  "type": "about:blank",
  "title": "La solicitud contiene datos inválidos",
  "status": 400,
  "detail": "Revisa los campos indicados.",
  "errors": {
    "capacidad": ["La capacidad debe ser mayor que cero."]
  }
}
```

- `404`: el aula no existe. Un identificador con formato inválido no coincide con la ruta y también responde `404`, como en el resto de la API.
- `401` y `403`: ver [Permisos](#permisos). No se producen hasta que existan cuentas y sesión.
- No hay `409` propio de las aulas en esta tarea, porque no hay unicidad ni eliminación.

## Permisos

Propuesta, pendiente de la implementación de cuentas y sesión (#84) y de que se reflejen en [identidad y permisos](../../reglas/identidad-y-permisos-m6.md):

| Operación | Quién puede |
|---|---|
| Crear y editar aulas (`POST`, `PUT`) | Administrador. |
| Consultar aulas (`GET`) | Cualquier usuario con sesión: administrador, coordinador, jefe de departamento y estudiante. |

Los coordinadores necesitan ver las aulas al completar las imparticiones de un horario (M6-07), y los estudiantes y coordinadores consultan la disponibilidad de aulas (M6-12). Sin sesión válida la API responde `401`; con sesión pero sin permiso, `403`. Los permisos se comprueban en el servidor, no solo ocultando opciones en la interfaz.

Mientras #84 no exista, estos endpoints no comprueban permisos, igual que el resto de la API de M4. Queda registrado en `AulasController` para activarlos después, y las pruebas de acceso permitido y denegado (401 y 403) se añadirán entonces.

La pantalla consulta `/api/sesion` de forma provisional: con `401` muestra el aviso de inicio de sesión y, mientras el endpoint no exista, interpreta el `404` como acceso de gestión para no bloquear la funcionalidad. Ese comportamiento deberá retirarse al activar #84; no es un control de seguridad.

## Fuera de alcance de esta tarea

- Eliminar o inactivar aulas.
- Tipo de aula y capacidad como límite de estudiantes.
- Asignar aulas a imparticiones y evitar solapamientos (M6-07).
- Disponibilidad y ocupación de aulas (M6-12).

## Decisiones pendientes

1. **Cómo se distingue un aula de otra.** `AULA` solo tiene `idAula`, `edificio` y `capacidad`: no hay nombre ni número. Dos aulas del mismo edificio y capacidad se ven iguales en pantalla y en los selectores de M6-07. Si el diseño se amplía con un nombre o número, habrá que añadirlo a este contrato (y decidir entonces si su duplicado en un edificio da `409`).
2. **Topes de validación.** Los valores de 100 caracteres y capacidad 1000 son propuestas.
3. **Permisos.** Quién crea y edita, y quién consulta (tabla de arriba).
4. **Estado `activo`.** Los demás recursos lo tienen, pero `AULA` no lo incluye en el diseño y añadirlo exige una migración. Se propone dejarlo fuera de M6-06.
