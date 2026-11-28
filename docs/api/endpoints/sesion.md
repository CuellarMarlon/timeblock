# Sesión

**Requisitos relacionados:** [identidad y permisos](../../reglas/identidad-y-permisos-m6.md) (#67).  
**Tarea:** cuentas e inicio de sesión, issue #84.  
**Convenciones comunes:** [Contrato de la API](../contrato-api.md).  
**Estado:** propuesta mínima, con lo que necesita el frontend para saber quién actúa y qué puede hacer. Quien implemente #84 puede ampliarla o corregirla. La gestión de cuentas (crear, desactivar, restablecer contraseña) se documenta aparte.

## Endpoints

| Método y ruta | Resultado |
|---|---|
| `POST /api/sesion` | Inicia sesión con usuario y contraseña. |
| `GET /api/sesion` | Devuelve la sesión actual. |
| `DELETE /api/sesion` | Cierra la sesión. |

Iniciar sesión:

```json
{
  "usuario": "ana.perez",
  "contrasena": "..."
}
```

Respuesta `200 OK`, igual que `GET /api/sesion`:

```json
{
  "cuentaId": "uuid",
  "usuario": "ana.perez",
  "rol": "coordinador",
  "personaId": "uuid",
  "nombre": "Ana Pérez"
}
```

- `rol` toma uno de estos valores: `administrador`, `estudiante`, `coordinador`, `jefeDepartamento`.
- `personaId` y `nombre` son `null` en la cuenta administrativa, que no está vinculada a una persona académica.
- Cada cuenta tiene un solo rol, el de su perfil. El administrador no obtiene permisos académicos por serlo.

## Cómo se mantiene la sesión

Se propone una cookie de sesión `HttpOnly`, que el servidor crea al iniciar sesión. Así el frontend no guarda contraseñas ni tokens y basta con llamar a la API desde el mismo origen (`/api`). La duración de la inactividad y el resto de detalles técnicos se definen al implementarla, como indican los acuerdos de identidad.

La sesión deja de ser válida cuando caduca por inactividad, se cierra, se desactiva la cuenta o se cambia o restablece su contraseña.

## Errores esperados

- `POST /api/sesion`
  - `400`: usuario o contraseña ausentes.
  - `401`: credenciales incorrectas o cuenta desactivada, con el mismo mensaje en ambos casos para no revelar cuál ocurrió.
- `GET /api/sesion`
  - `401`: no hay sesión válida.
- `DELETE /api/sesion`
  - `204`: sesión cerrada. Si no había sesión, también responde `204`.

## Efecto en el resto de la API

A partir de que se implemente #84, cualquier endpoint protegido responde:

- `401` si la petición no tiene una sesión válida.
- `403` si la sesión es válida pero el rol no tiene permiso para esa operación.

El frontend usa el `rol` para mostrar u ocultar opciones, pero esa comprobación no sustituye a la del servidor, que se hace tanto en listados como al acceder directamente a un recurso.

La tabla de códigos del [contrato común](../contrato-api.md#errores) debe actualizarse entonces: hoy describe `403` como autorización de negocio del coordinador y no incluye `401`.

## Fuera de alcance de este documento

- Crear cuentas, desactivarlas o reactivarlas y restablecer o cambiar contraseñas.
- Las autorizaciones de coordinadores por programa, que siguen como están en M4 hasta que se apliquen los permisos de M6.
- La duración de la inactividad.
