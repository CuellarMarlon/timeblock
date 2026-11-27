# Identidad y permisos para M6

Este documento recoge los acuerdos de la [issue #67](https://github.com/CuellarMarlon/timeblock/issues/67). Se basan en el [diseño de la base de datos](../Diseño%20de%20la%20Base%20de%20Datos.pdf), especialmente en RF21, RF25–RF34, RF45 y RII01–RII08.

Son reglas para implementar en M6, no una descripción de funcionalidades que ya estén terminadas. En M4 existen personas, perfiles y autorizaciones de coordinadores, pero la API todavía no comprueba quién envía cada solicitud. Por ejemplo, al crear un horario comprueba la autorización del `coordinadorId` recibido, pero no verifica que ese coordinador sea el usuario conectado.

## Cuentas e inicio de sesión

- Timeblock gestionará sus propias cuentas con **nombre de usuario único y contraseña**, sin depender de un proveedor externo ni exigir correo electrónico. Las contraseñas se guardarán como hashes seguros, nunca en texto plano.
- Cada persona académica podrá tener como máximo una cuenta y podrá existir sin ella. La cuenta se vincula a la persona y a su perfil existente: estudiante, coordinador o jefe de departamento.
- Se mantiene el perfil único que establece el contrato actual. Por ahora no se podrá convertir una persona de un tipo de perfil a otro.
- Habrá **una sola cuenta administrativa independiente**, sin necesidad de crear un cuarto perfil académico ni vincularla a una persona académica.
- El administrador podrá crear una persona y su cuenta en un mismo proceso, o crear una cuenta para una persona ya registrada. No se duplicarán personas para crear cuentas.
- Al preparar el entorno, se creará la primera cuenta administrativa con credenciales proporcionadas mediante configuración, sin guardarlas en el repositorio. Si se pierde su contraseña, se podrá restablecer mediante un comando ejecutado en el entorno.
- El administrador podrá restablecer contraseñas y desactivar o reactivar cuentas. Una cuenta desactivada no podrá iniciar sesión ni seguir usando una sesión abierta desde la siguiente petición. Sus datos e historial se conservan.
- El administrador comunica al usuario su contraseña inicial o restablecida por un medio fuera del sistema. El usuario podrá cambiarla, pero no estará obligado a hacerlo. Al cambiar o restablecer una contraseña se invalidarán las sesiones anteriores de esa cuenta.
- El usuario podrá cerrar sesión y esta caducará tras un período de inactividad. La duración y los detalles técnicos de la sesión se definirán al implementarla.

La API debe comprobar la sesión y obtener de ella la persona que actúa. Un identificador enviado por el cliente no demuestra identidad. Si se mantiene un campo de actor en un contrato existente, deberá coincidir con la persona autenticada. Los permisos se comprueban en el servidor, tanto en listados como al acceder directamente a un recurso.

## Qué puede hacer cada perfil

| Perfil | Acceso acordado |
| --- | --- |
| **Administrador** | Gestiona personas, cuentas, programas, asignaturas, matrículas y autorizaciones de coordinadores. Puede actualizar los datos de los perfiles existentes. No genera, valida ni revisa horarios, ni registra asistencia o resuelve solicitudes. No consulta por defecto asistencia individual, solicitudes ni reportes académicos. |
| **Estudiante** | Consulta sus datos personales, horarios asignados, asistencia y solicitudes propias. Puede crear solicitudes de cambio y consultar su resultado. También puede consultar disponibilidad general de horarios y aulas. No consulta datos individuales de otros estudiantes ni reportes agregados. |
| **Coordinador** | Gestiona sus propios bloques y trabaja con horarios, asignaciones, asistencia, solicitudes y reportes de los programas que atiende, según las reglas de las secciones siguientes. También puede consultar disponibilidad general. |
| **Jefe de departamento** | Consulta y revisa horarios de todos los programas. Puede consultar asistencia y reportes de todos los programas, incluidos los comparativos, para sus funciones; no registra ni modifica asistencia. Aprueba un horario o solicita una nueva generación según el [flujo de #71](reglas-negocio-m6.md). No genera ni valida por ser jefe. |

El administrador modifica los datos personales; cada usuario consulta los suyos y puede cambiar su contraseña. El acceso del jefe es global por ahora: el diseño no relaciona jefes con departamentos o programas concretos. Si se necesita limitarlo en el futuro, habrá que ampliar el modelo.

## Coordinadores y programas

Un coordinador **atiende un programa** cuando tiene una autorización vigente para él, aunque `puedeGenerar` y `puedeValidar` estén ambos desactivados. Esto permite, entre otras funciones acordadas, consultar los horarios del programa, incluso los creados por otros coordinadores.

El administrador gestiona esas autorizaciones: programa, vigencia y permisos. Gestionarlas no lo convierte en coordinador ni le permite realizar sus operaciones académicas.

Para las siguientes operaciones no basta con atender el programa:

- **Generar un horario:** el coordinador conectado necesita una autorización vigente para ese programa con `puedeGenerar = true` en la fecha de generación (RII01).
- **Validar un horario:** necesita una autorización vigente para el programa del horario con `puedeValidar = true` en la fecha de validación (RII02). La validación debe conservar la autorización utilizada.

La autorización se identifica por coordinador, programa y fecha de inicio. En el dominio actual, el inicio y el fin son inclusivos; un fin vacío indica que no tiene fecha de finalización. Generar y validar son permisos distintos: tener uno no implica tener el otro.

Cuando vence o se retira una autorización, el coordinador pierde el acceso que dependía de ella desde ese momento. Sus actuaciones anteriores se conservan. Los cambios y bajas deben respetar las reglas que protegen los horarios ya vinculados y, al implementar validaciones, habrá que proteger también su trazabilidad histórica.

## Bloques y horarios

- El coordinador conectado es el creador de sus bloques. Solo el creador puede editarlos y clasificarlos.
- Solo el creador puede asociar o desasociar sus bloques de programas que atienda, respetando las reglas de integridad y conservación aplicables. La asignatura del bloque debe pertenecer al programa (RII03).
- Otros coordinadores pueden consultar y usar esos bloques en horarios de programas que atiendan, siempre que estén asociados al programa correspondiente.
- Un coordinador que atiende el programa puede asignar sus horarios a estudiantes matriculados en él. El estudiante consulta sus asignaciones, pero no se asigna horarios por su cuenta.
- Solo se asignan horarios `APROBADO`, respetando la matrícula y las restricciones de vigencia y solapamiento del diseño (RII08). El coordinador del programa puede cerrar asignaciones; antes de desmatricular deben cerrarse las asignaciones activas en ese programa.
- Estudiantes y coordinadores pueden consultar la disponibilidad y ocupación de horarios y aulas. La consulta muestra la información necesaria —como programa, período, aula, fecha e intervalo— sin mostrar listas de estudiantes ni su asistencia.

## Asistencia

- El coordinador registra, actualiza y consulta la asistencia de los estudiantes en horarios de los programas que atiende.
- El estudiante consulta únicamente su propia asistencia. El jefe puede consultarla para revisión y reportes en todos los programas, pero no modificarla.
- Solo puede registrarse asistencia para una impartición existente y un estudiante con una asignación vigente al horario en la fecha de impartición (RII05).
- La falta de un registro no equivale a una ausencia confirmada.

## Solicitudes de cambio

- El estudiante puede crear solicitudes sobre sus propios horarios asignados y consultar su estado y resultado. La asignación debe estar vigente en la fecha de solicitud (RII06).
- Puede solicitar apoyo a un coordinador adicional. Para elegirlo, verá los datos necesarios para identificar a los coordinadores que atienden el programa del horario original, sin acceso al directorio completo de personas ni a los datos de sus cuentas.
- Pueden gestionar y resolver la solicitud el coordinador asignado al estudiante o el coordinador adicional solicitado, siempre que atiendan el programa del horario original. Si el asignado no lo atiende, el estudiante deberá solicitar apoyo a uno que sí lo atienda.
- Mientras la solicitud esté pendiente o en revisión, el estudiante puede añadir o cambiar el coordinador de apoyo por otro que atienda el programa. Esto permite continuar si un coordinador deja de estar autorizado.
- El estudiante o cualquiera de los coordinadores habilitados para atenderla pueden cancelar una solicitud pendiente o en revisión. Si cancela un coordinador, debe indicar el motivo; para el estudiante es opcional. El resultado se muestra al estudiante y no se modifican las asignaciones. Una solicitud aprobada, rechazada o cancelada ya no puede cancelarse.
- Si una solicitud se cancela o rechaza, el estudiante puede presentar otra mientras siga teniendo una asignación válida. No puede tener dos solicitudes activas para el mismo horario original; a estos efectos, activas son las pendientes y en revisión.
- Al aprobar un cambio, se registra al coordinador conectado que lo resolvió y se conserva la relación entre solicitud, horario original y resultante. El cierre de la asignación anterior y la creación de la nueva se hacen en una misma transacción (RII07), comprobando los conflictos y RII08.
- La resolución y la cancelación deben protegerse ante operaciones simultáneas: una solicitud ya cancelada no puede aprobarse.

## Reportes y exportación

- El coordinador consulta reportes de los programas que atiende. Para comparar programas debe atender todos los incluidos.
- El jefe consulta reportes de todos los programas. El estudiante no accede inicialmente a reportes agregados ni a datos de otros estudiantes.
- Todos los tipos de usuario pueden exportar a PDF únicamente la información que tienen permiso para consultar (RF45). La exportación no debe permitir ampliar ese acceso.
- Los filtros de acceso se aplican antes de paginar, contar o agregar resultados, también cuando se consulta un recurso por su identificador.

## Casos que hay que comprobar en M6

1. Una solicitud sin sesión válida recibe `401`; un usuario autenticado sin permiso recibe `403`, de acuerdo con el contrato que se defina. No basta con ocultar opciones en la interfaz.
2. Una cuenta desactivada o una sesión anterior a un cambio de contraseña deja de funcionar; reactivar la cuenta conserva su historial.
3. Un estudiante no puede consultar datos, asistencia o solicitudes ajenos cambiando identificadores, filtros o direcciones.
4. Un coordinador puede consultar horarios de otro coordinador en un programa que atiende, pero no acceder a los de un programa que no atiende. Se comprueban los límites de vigencia y las autorizaciones con ambos indicadores desactivados.
5. No se puede generar o validar haciéndose pasar por otro coordinador. Se comprueban programa, vigencia y permiso específico; también las modificaciones concurrentes de la autorización.
6. Solo el creador puede editar o asociar su bloque; se comprueba que atienda el programa y que se respete RII03.
7. La asistencia respeta RII05 y la ausencia de registro no se presenta como ausencia confirmada.
8. Solo el estudiante dueño y los coordinadores habilitados acceden a la solicitud según sus funciones. Se prueban el cambio de apoyo, la cancelación, su motivo y la carrera entre cancelación y aprobación.
9. Una nueva solicitud se permite tras rechazo o cancelación si la asignación sigue vigente, pero no si ya existe otra activa para el mismo horario original.
10. Reportes, listados, totales y PDF respetan el alcance de cada usuario, también en comparaciones entre programas y consultas directas.
11. El administrador puede gestionar cuentas y autorizaciones, pero no obtiene por ello permisos académicos. El jefe puede revisar horarios globalmente, pero no generar ni validar por ser jefe.

## Trabajo posterior

- Implementar las cuentas, el inicio y cierre de sesión, la recuperación de la cuenta administrativa y las comprobaciones de permisos en backend y frontend. Definir la duración de inactividad y los detalles técnicos de sesión durante la implementación.
- Actualizar el contrato de la API y los endpoints existentes cuando se aplique la identidad autenticada. El contrato de M4 describe el comportamiento anterior y no debe interpretarse como una protección ya implementada.
- Implementar el ciclo de estados, validación, revisión y asignación recogido en las [reglas de horarios de #71](reglas-negocio-m6.md).
- Planificar y distribuir las tareas de M6 en [#69](https://github.com/CuellarMarlon/timeblock/issues/69). La concurrencia de RII01 se trata en [#77](https://github.com/CuellarMarlon/timeblock/issues/77); RII02 deberá protegerse al implementar la validación.
- Coordinar la implementación de reportes y exportación con la planificación de M6, aplicando los permisos acordados aquí.
