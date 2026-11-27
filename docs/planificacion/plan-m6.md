# Plan de M6

Propuesta nueva para la [issue #69](https://github.com/CuellarMarlon/timeblock/issues/69). Sustituye el plan anterior y parte de:

- [Diseño de la base de datos](../Diseño%20de%20la%20Base%20de%20Datos.pdf): requisitos, relaciones y restricciones.
- [Identidad y permisos](../reglas/identidad-y-permisos-m6.md), de #67.
- [Reglas de horarios](../reglas/reglas-negocio-m6.md), de #71.
- [Revisión de cobertura de M5](../revisiones/cobertura-diseno-m5.md): fotografía del cierre de M4, no inventario actualizado.
- [Bajas y eliminación de M4](../revisiones/bajas-y-eliminacion-m4.md), de #66.

**Estado:** las 30 tareas están creadas en el [proyecto de GitHub](https://github.com/users/CuellarMarlon/projects/2), con estado Backlog. La sección 7 relaciona los identificadores `M6-xx` con las issues reales y la sección 8 registra el reparto inicial. Las prioridades, estimaciones y calendario aún deben acordarse y el equipo puede ajustar los responsables. Crear una issue para una tarea no significa que la funcionalidad ya esté implementada.

## 1. Punto de partida

En M4 existen personas, perfiles, autorizaciones, catálogos, matrículas, bloques y armado manual de horarios. #65 resolvió los selectores de matrícula y #66 añadió bajas y protección de integridad. La revisión de #68 se hizo antes de esos cambios.

La migración `ProtegerVigenciaHorario` de #66 bloquea la autorización al crear un horario y protege los horarios existentes ante cambios que los invaliden. `HorarioRepositoryIntegrationTests.CrearHorarioYModificarAutorizacion_Concurrentes_NoInvalidanHorario` cubre los dos órdenes de esa carrera. No se han vuelto a ejecutar esas pruebas para preparar este plan. Por su parte, `TimeblockDbContext` todavía consulta y luego inserta las fechas que faltan; la carrera de alta de `FECHA` sigue necesitando comprobación y corrección.

La issue [#77](https://github.com/CuellarMarlon/timeblock/issues/77) se actualizó como M6-01, conservando su historial y responsable. La protección de RII01 fue abordada por #66; el alta concurrente de `FECHA` sigue pendiente de comprobar y corregir.

Las fechas de los hitos M6–M8 estaban vencidas al preparar este documento; deben confirmarse con el equipo antes de establecer un calendario real.

## 2. Puntos que requieren atención antes de implementar

Estos puntos no autorizan a cambiar el PDF sin revisar su efecto. Se investigan en las tareas indicadas y, si requieren una decisión funcional o una modificación formal del diseño, se documenta y consulta antes de implementarla.

1. **Cobertura, edición y candidatos:** el acuerdo calcula la cobertura sobre todos los bloques seleccionados, pero no conserva la selección completa. Los candidatos omitidos no pueden reconstruirse del horario; tampoco puede demostrarse que una selección introducida después sea la original. Hay que resolver cómo verificar cobertura y orden al editar o enviar a validar sin afirmar que se comprobó algo que no consta. Afecta M6-08 y M6-10.
2. **Fechas y plantilla semanal:** `periodoAcademico` conserva meses y años, no los días exactos de inicio y fin ni las franjas candidatas. Hay que definir cómo verificar una semana fija tras editar sin inventar fechas que no estén disponibles. Afecta M6-08 y M6-10.
3. **Autorizaciones históricas:** la regla actual impide desactivar `puedeGenerar` en una autorización ya usada por horarios. La matriz de #67 exige que el permiso pueda dejar de dar acceso sin alterar actuaciones históricas. También hay que preservar la autorización utilizada en validaciones. La solución debe respetar el diseño y proteger concurrencia, no reescribir silenciosamente la historia. Afecta M6-05 y M6-13.
4. **Vigencias y asistencia:** RII05 y RII06 hablan de vigencia en una fecha; las asignaciones conservan marcas de tiempo. Hay que concretar los límites de inicio y fin y cómo se consulta una fecha de impartición sin perder historial ni permitir dobles asignaciones. Afecta M6-15, M6-17 y M6-19.
5. **ESQUEMA_LLAVE:** el PDF (§6.9.2 y §7.2.21) la materializa como tabla física. Existe parcialmente, pero no define cuándo ni con qué significado se llena. No se elimina ni se insertan combinaciones arbitrarias; las referencias se estudian según las claves completas. Afecta M6-21.
6. **Cálculos de reportes:** acordar y documentar los denominadores, el tratamiento de tardanzas y ausencias justificadas, los registros faltantes y las correlaciones sin datos suficientes. RI27 prohíbe contar la ausencia de registro como inasistencia. Afecta M6-29.
7. **Cambios en bloques utilizados:** un cambio posterior de asignatura, tipo o complejidad puede alterar la interpretación de horarios y reportes históricos. Analizar las reglas permitidas y las restricciones existentes antes de decidir cómo protegerlos, sin modificar el diseño por suposición. Afecta M6-28, M6-09 y los reportes.

## 3. Tareas propuestas

Cada tarea incluye backend, persistencia, API o frontend según corresponda, documentación de contrato y pruebas de su alcance. Las tareas que manipulan datos relacionados se verifican también ante fallos y concurrencia cuando sea pertinente.

### A. Base e identidad

**M6-01 — Concurrencia de fechas y regresión de RII01** · D8, D10; issue #77 actualizada.

- Corregir el alta concurrente de `FECHA` sin perder atomicidad; comprobar dos solicitudes con la misma fecha y el estado final en PostgreSQL.
- Conservar y ejecutar la protección y las pruebas de #66 para RII01; probar los cambios que introduzca la generación automática y mantener los errores esperados.

**M6-02 — Cuentas e inicio de sesión** · #67; depende de las decisiones técnicas de sesión.

- Una cuenta académica como máximo por persona, usuario único, contraseña con hash seguro y una cuenta administrativa independiente.
- Inicialización y recuperación del administrador desde el entorno; inicio y cierre de sesión, caducidad por inactividad y cambio de contraseña.
- Desactivación, reactivación y restablecimiento con invalidación de sesiones, incluidas sesiones ya abiertas; pruebas de credenciales y acceso.

**M6-03 — Panel de administración de cuentas** · #67; depende de M6-02.

- Crear persona y cuenta o vincular una cuenta a una persona existente; evitar duplicados y manejar fallos sin dejar altas parciales.
- Consultar vínculos, restablecer contraseñas y activar o desactivar cuentas; comunicar los errores en la interfaz.

**M6-27 — Interfaz de acceso y sesión** · #67; depende del contrato de M6-02.

- Implementar pantallas de inicio de sesión, consulta de datos personales y cambio opcional de contraseña, cierre de sesión y manejo de credenciales inválidas, sesión vencida o cuenta desactivada.
- Adaptar navegación y rutas a la sesión y al perfil; probar recuperación de la sesión y sus errores sin confiar en la interfaz como control de permisos.

**M6-04 — Permisos de API y módulos existentes** · RF09, RF26, RF29, RF45; depende de M6-02.

- Aplicar en backend identidad, perfiles, programa atendido y propiedad a las operaciones actuales; administrador gestiona datos generales y autorizaciones según #67.
- Las acciones usan el actor autenticado; las consultas, totales y acceso directo a recursos respetan el alcance antes de paginar. Probar 401, 403 y suplantación por perfil.
- Conservar RII03 y las reglas de baja al restringir la edición y asociación de bloques por su creador.

**M6-05 — Vigencias e historial de autorizaciones** · RF04, RF13, RF21, RII01, RII02; depende de M6-04 y del punto 3 de la sección 2.

- Resolver la pérdida de permisos futuros sin invalidar generación ni validación históricas; documentar cualquier adaptación necesaria al diseño antes de aplicarla.
- Probar vigencia inclusiva, fecha UTC, cambios concurrentes y trazabilidad de autorizaciones usadas. Generar y validar son permisos independientes.

**M6-28 — Consistencia histórica de bloques utilizados** · RF09–RF11, RF14, RF35–RF42; depende de M6-04 y del punto 7 de la sección 2.

- Analizar qué ocurre al cambiar asignatura, tipo, complejidad o descripción de un bloque usado en horarios y reportes. Definir y documentar una solución compatible con el PDF y las operaciones de #66 antes de implementarla.
- Probar que las modificaciones permitidas conservan las reglas de programa, la interpretación de los criterios y la coherencia de los datos históricos; rechazar o gestionar las no permitidas según la solución acordada.

### B. Aulas y generación

**M6-06 — Gestión de aulas** · RI16, §7.2.13.

- Alta, edición y consulta de aulas con edificio y capacidad positiva, con API y pantalla según los permisos acordados.
- No clasificar aulas por tipo ni aplicar capacidad como límite de estudiantes; comprobar validaciones y errores.

**M6-07 — Imparticiones completas y protección de aulas** · RF16, RF17, RI14–RI16, RII04; depende de M6-01 y M6-06.

- Guardar fecha, inicio, fin, período académico y aula; cumplir la clave `(horario, bloque, fecha)` y la alternativa de aula/fecha/inicio del diseño.
- Impedir solapamientos de aula incluso ante concurrencia; permitir intervalos contiguos. Impedir también clases solapadas en el mismo horario.
- Probar persistencia, API y carreras con dos conexiones; no asumir que el índice único de M4 cubre todos los intervalos.

**M6-08 — Datos y reglas de generación semestral** · RF12, RF14, RI13; depende de la sección 2, puntos 1 y 2, y de M6-07.

- Resolver y documentar la comprobación de cobertura y orden sin guardar la selección de candidatos, y la verificación de fechas y plantilla. Cualquier cambio del diseño requiere decisión previa.
- Persistir los parámetros exigidos; interpretar la cantidad como imparticiones, el período como `MM-AAAA - MM-AAAA` y `periodoImp` como el mismo período. Migrar los datos y el contrato sin reinterpretar silenciosamente el conteo de M4.
- Probar cobertura por asignatura y redondeo hacia arriba, proporciones y restos mayores, promedios y límites, orden estricto y preferencias acordadas.

**M6-09 — Generador automático para el semestre** · RF12–RF15; depende de M6-05, M6-07 y M6-08.

- Seleccionar entre candidatos ordenados y construir espacios semanales fijos de asignatura, hora y aula, con bloques y tipos que pueden variar por semana. Repetir solo dentro de las fechas indicadas; no añadir feriados ni excepciones.
- Aplicar cantidad, proporciones, cobertura, complejidad, orden y restricciones. Priorizar uso de candidatos y después estabilidad de tipos solo entre soluciones válidas.
- Detectar imposibilidades y conservar atomicidad; distinguir búsqueda interrumpida de ausencia demostrada de solución. Probar criterios, claves, prioridades entre soluciones y casos imposibles; medir el comportamiento con un semestre y volumen de candidatos representativos.

**M6-10 — Crear, corregir y regenerar horarios** · RF13, RF14, RF16; depende de M6-04, M6-08 y M6-09.

- API de generación y corrección por el creador autorizado; al cambiar parámetros se regenera el mismo horario y se vuelven a seleccionar y ordenar candidatos.
- Conservar programación y parámetros válidos ante fallo; comprobar que las correcciones mantienen las reglas acordadas, una vez resueltos los puntos de la sección 2.
- Retirar la creación manual de M4 y preparar la limpieza puntual acordada de horarios de prueba y asociaciones, sin borrar personas, programas ni bloques. No ejecutar la limpieza sobre datos no identificados como prueba.

**M6-11 — Pantalla de generación y consulta de horarios** · RF12–RF16; depende de los contratos de M6-09 y M6-10.

- Selección y orden de candidatos, franjas, fechas y parámetros; mostrar errores y resultados de criterios.
- Presentar asignaturas por semana, con detalle de bloque y posibilidad de consultar el período. Probar estados, navegación y correcciones sin atribuirse permisos desde la interfaz.

**M6-12 — Disponibilidad y ocupación** · RF27, RI16, RNF01; depende de M6-04, M6-06 y M6-07.

- Consultar disponibilidad y ocupación de horarios y aulas por fecha y franja sin exponer estudiantes ni asistencia.
- Implementar índices y caché de aplicación previstos por RNF01, con invalidación cuando cambien los datos. Medir y documentar consultas concurrentes con tiempos inferiores a un segundo, cargas, resultados y entorno.

### C. Validación, revisión y asignación

**M6-13 — Validación y estados de horarios** · RF21, RF22, RI19–RI20, RII02; depende de M6-05, M6-07 y M6-10.

- Implementar `VALIDACION`, guardar autorización, fecha y observaciones y proteger la comprobación de RII02 ante cambios concurrentes.
- Implementar envío a validación, devolución a `GENERADO`, cancelación autorizada y transición automática `VALIDADO` → `EN_REVISION`; congelar datos validados y proteger las transiciones ante carreras.

**M6-14 — Revisión del jefe y regeneración vinculada** · RF18–RF20, RF22, RI17–RI18; depende de M6-09 y M6-13.

- Crear `REVISION`; registrar jefe, decisión, observaciones y criterios en texto. Aprobar o solicitar nueva generación según #71.
- Vincular un único sustituto a cada revisión, conservar el original y cambiarlo a `RECHAZADO` solo si se genera el sustituto. Probar fallos, concurrencia y acceso de jefes.

**M6-15 — Asignaciones y desmatriculación** · RF23–RF24, RI21, RII08; depende de M6-04, M6-14 y del punto 4 de la sección 2.

- Implementar `HORARIO_ESTUDIANTE`, asignación individual o múltiple de horarios aprobados a estudiantes matriculados y cierre con historial.
- Impedir RII08 incluso con solicitudes concurrentes y bloquear desmatriculación con asignaciones activas. Informar resultados por estudiante sin superar el alcance del usuario.

**M6-16 — Pantallas de validación, revisión y asignación** · RF18–RF26; depende de los contratos de M6-13 a M6-15.

- Bandejas y acciones según perfil; criterios de revisión, validación, asignación múltiple y cierre. Mostrar solo operaciones permitidas, sin sustituir controles de API.
- Mostrar horarios propios al estudiante y los del programa atendido al coordinador, incluidos los creados por otros.

### D. Asistencia y solicitudes

**M6-17 — Registro y consulta de asistencia** · RF28–RF29, RI25–RI27, RII05; depende de M6-04, M6-07 y M6-15.

- Crear `ASISTENCIA` y sus cuatro estados; comprobar impartición y asignación vigente en la fecha. Un registro ausente no es una ausencia confirmada.
- Permitir registro y modificación por coordinador del programa; consultas del propio estudiante, coordinadores autorizados y jefes. Probar filtros y acceso directo.

**M6-18 — Pantallas de asistencia** · RF28–RF29; depende del contrato de M6-17.

- Registrar, modificar y consultar por estudiante, horario y bloque; distinguir visualmente registros faltantes y estados de asistencia.

**M6-19 — Crear y consultar solicitudes de cambio** · RF30–RF31, RF34, RI28–RI29, RII06; depende de M6-04 y M6-15.

- Crear `SOLICITUD_CAMBIO` y sus estados; comprobar propiedad y asignación vigente, impedir dos solicitudes activas para el mismo horario original y conservar motivo y fecha.
- Permitir seleccionar, añadir o cambiar apoyo habilitado mientras esté pendiente o en revisión; consultar estado y resultado propios. Proteger duplicados concurrentes.

**M6-20 — Gestionar y resolver solicitudes** · RF32–RF33, RI30–RI31, RII07–RII08; depende de M6-15 y M6-19.

- Permitir actuar solo al coordinador asignado o al apoyo solicitado que atienda el programa. Registrar al resolutor autenticado.
- Implementar cancelación autorizada y motivo obligatorio para coordinador; impedir cancelar una ya resuelta y proteger carrera entre cancelación y resolución.
- Al aprobar, cerrar la asignación anterior y crear la nueva en una transacción, conservando la relación con los horarios y comprobando conflictos. Probar fallos a mitad y concurrencia.

**M6-21 — ESQUEMA_LLAVE y referencias restantes** · §6.9.2 y §7.2.21; depende de las relaciones de M6-13, M6-14, M6-15 y M6-19.

- Conservar la relación física y su clave. Revisar qué referencias pueden expresarse con claves completas; no asumir que una fecha aislada identifica una asignación.
- Analizar y documentar el significado y alimentación de sus filas antes de crear operaciones o datos. Probar las restricciones efectivamente acordadas; no afirmar pérdida nula de datos reales solo porque la tabla exista.

**M6-22 — Pantallas de solicitudes** · RF30–RF34; depende de los contratos de M6-19 y M6-20.

- Formulario y consulta del estudiante; selección de apoyo; bandeja, resolución y cancelación del coordinador; mostrar resultado y motivos según el estado.

### E. Reportes y datos históricos

**M6-23 — Reportes de horarios y bloques** · RF35–RF37, RF39, RI32–RI34, RI36; depende de M6-09, M6-14 y M6-28.

- Listar horarios generados por programa con creador, fecha y parámetros; bloques más impartidos en horarios finales por complejidad y asignatura; y horarios validados por coordinador con fecha y observaciones.
- Comparar horarios de varios programas por asignatura y complejidad y sus criterios de equilibrio. Contar las imparticiones de los bloques y aplicar los permisos antes de agregar o comparar. Probar cada consulta con datos conocidos.

**M6-29 — Reportes de asistencia y desempeño** · RF38, RF40–RF42, RI35, RI37–RI39; depende de M6-17, M6-20 y M6-28.

- Implementar desempeño por horario y complejidad, correlación entre complejidad y asistencia por programa, los diez bloques con mayor inasistencia y la comparación de solicitantes con sus grupos en los mismos programas y períodos.
- Documentar tasas, denominadores, tratamiento de tardanzas y ausencias justificadas, registros faltantes y correlaciones sin datos suficientes. Respetar RI27 y los permisos antes de agregar; probar cada informe con datos conocidos.

**M6-24 — Tablas, gráficos y exportación PDF** · RF43–RF45, RI40; depende de M6-04 y los contratos de M6-23 y M6-29.

- Presentar reportes en tablas y gráficos; permitir ordenar cada columna y explicar el alcance de la ordenación si hay paginación.
- Exportar únicamente información consultable por el usuario: vistas personales del estudiante, información académica autorizada de coordinador y jefe, e información administrativa permitida del administrador. Probar cada perfil, filtros y accesos directos.

**M6-25 — Modelo del almacén analítico** · RI41, RNF02, §7.4; depende de los contratos de datos de M6-07 y M6-17.

- Diseñar e implementar almacenamiento independiente en ClickHouse para ocupación de aulas y asistencia histórica por bloque, franja, edificio y período.
- Documentar las claves de origen y el nivel de detalle de los datos; probar consultas agregadas sobre ejemplos conocidos y mantener desacoplado el modelo operacional.

**M6-30 — Sincronización del histórico analítico** · RF46, RI41, RNF02, §7.4; depende de M6-07, M6-17 y M6-25.

- Implementar una sincronización periódica de ocupación y asistencia que conserve la correspondencia con el origen y registre última actualización y errores.
- Probar cargas completas, reintentos y reprocesamiento sin duplicados, fallos y consultas agregadas de los datos sincronizados.

**M6-26 — Integración, demostración y cierre de M6** · todos los bloques; depende de las funcionalidades anteriores.

- Preparar datos y recorrido reproducible por cuentas y perfiles: generación semestral, validación, revisión, asignación, asistencia, solicitud, reportes y sincronización.
- Ejecutar pruebas de backend y frontend, integración con PostgreSQL y ClickHouse y un recorrido completo; registrar versiones, resultados, limitaciones y correcciones.
- Contrastar RF01–RF46, RI01–RI41 aplicables, RNF01–RNF03, RII01–RII08 y los acuerdos de M5 con evidencia de implementación y prueba o demostración. RI08 no aparece en el PDF. No marcar como completo un requisito solo por existir una tabla o pantalla.

## 4. Dependencias y orden de trabajo

1. Revisar el plan y resolver las cuestiones que afectan al diseño de generación y autorizaciones. Actualizar el calendario y revisar el reparto inicial con el equipo.
2. Iniciar en paralelo M6-01, M6-02 y M6-06; diseñar las pantallas y contratos con los responsables de cada módulo. M6-21 requiere las tablas que faltan, aunque su análisis puede empezar antes. M6-27 puede avanzar con el contrato de sesiones.
3. Completar permisos, aulas y reglas de impartición para habilitar el generador; después crear, corregir y consultar horarios.
4. Construir validación y revisión, seguidas de asignaciones. Asistencia y solicitudes se apoyan en las asignaciones; reportes requieren los datos de los flujos que analizan.
5. El diseño de ClickHouse (M6-25) puede avanzar en paralelo, pero la sincronización (M6-30) necesita sus fuentes de asistencia y ocupación. Cerrar con integración, demostración y comprobación requisito por requisito.

No se fija un camino crítico en jornadas ni se asigna una prioridad numérica definitiva sin estimaciones del equipo. El generador semestral, los permisos transversales, las restricciones concurrentes y la sincronización son riesgos de esfuerzo importantes y deben valorarse antes de repartir.

## 5. Cobertura del alcance

| Requisitos o reglas | Tareas |
| --- | --- |
| RF01–RF08, RF10–RF11 y datos maestros de M4 | Existentes; permisos en M6-04, consistencia en M6-28 y regresión en M6-26 |
| RF09 | M6-04 y M6-28 |
| RF12–RF17, RI12–RI16, RII01, RII03–RII04 | M6-01, M6-05 a M6-12 |
| RF18–RF22, RI17–RI20, RII02 | M6-05, M6-13–M6-14, M6-16 |
| RF23–RF26, RI21–RI24, RII08 | M6-15–M6-16 |
| RF27, RNF01 | M6-12 |
| RF28–RF29, RI25–RI27, RII05 | M6-17–M6-18 |
| RF30–RF34, RI28–RI31, RII06–RII08 | M6-19–M6-20, M6-22 |
| RF35–RF45, RI32–RI40 | M6-23–M6-24, M6-29 |
| RF46, RI41, RNF02 | M6-25, M6-30 |
| ESQUEMA_LLAVE, §6.9.2 y §7.2.21 | M6-21 |
| RNF03, arquitectura y pruebas | Todas las tareas; integración en M6-26 |
| Cuentas, inicio de sesión y permisos acordados en #67 | M6-02–M6-05, M6-27 |

## 6. Pasos para cerrar #69

1. Revisar con el equipo las tareas y resolver las cuestiones funcionales o de diseño de la sección 2.
2. Acordar calendario, prioridades y estimaciones, y revisar el reparto inicial con el equipo; registrar los acuerdos alcanzados.
3. Verificar que el proyecto y las issues mantengan sus dependencias y cobertura del PDF; cerrar #69 solo cuando se cumplan sus criterios.

## 7. Issues de M6

| Tarea | Issue | Tarea | Issue | Tarea | Issue |
| --- | --- | --- | --- | --- | --- |
| M6-01 | [#77](https://github.com/CuellarMarlon/timeblock/issues/77) | M6-11 | [#92](https://github.com/CuellarMarlon/timeblock/issues/92) | M6-21 | [#102](https://github.com/CuellarMarlon/timeblock/issues/102) |
| M6-02 | [#83](https://github.com/CuellarMarlon/timeblock/issues/83) | M6-12 | [#93](https://github.com/CuellarMarlon/timeblock/issues/93) | M6-22 | [#103](https://github.com/CuellarMarlon/timeblock/issues/103) |
| M6-03 | [#84](https://github.com/CuellarMarlon/timeblock/issues/84) | M6-13 | [#94](https://github.com/CuellarMarlon/timeblock/issues/94) | M6-23 | [#104](https://github.com/CuellarMarlon/timeblock/issues/104) |
| M6-04 | [#85](https://github.com/CuellarMarlon/timeblock/issues/85) | M6-14 | [#95](https://github.com/CuellarMarlon/timeblock/issues/95) | M6-24 | [#105](https://github.com/CuellarMarlon/timeblock/issues/105) |
| M6-05 | [#86](https://github.com/CuellarMarlon/timeblock/issues/86) | M6-15 | [#96](https://github.com/CuellarMarlon/timeblock/issues/96) | M6-25 | [#106](https://github.com/CuellarMarlon/timeblock/issues/106) |
| M6-06 | [#87](https://github.com/CuellarMarlon/timeblock/issues/87) | M6-16 | [#97](https://github.com/CuellarMarlon/timeblock/issues/97) | M6-26 | [#107](https://github.com/CuellarMarlon/timeblock/issues/107) |
| M6-07 | [#88](https://github.com/CuellarMarlon/timeblock/issues/88) | M6-17 | [#98](https://github.com/CuellarMarlon/timeblock/issues/98) | M6-27 | [#108](https://github.com/CuellarMarlon/timeblock/issues/108) |
| M6-08 | [#89](https://github.com/CuellarMarlon/timeblock/issues/89) | M6-18 | [#99](https://github.com/CuellarMarlon/timeblock/issues/99) | M6-28 | [#109](https://github.com/CuellarMarlon/timeblock/issues/109) |
| M6-09 | [#90](https://github.com/CuellarMarlon/timeblock/issues/90) | M6-19 | [#100](https://github.com/CuellarMarlon/timeblock/issues/100) | M6-29 | [#110](https://github.com/CuellarMarlon/timeblock/issues/110) |
| M6-10 | [#91](https://github.com/CuellarMarlon/timeblock/issues/91) | M6-20 | [#101](https://github.com/CuellarMarlon/timeblock/issues/101) | M6-30 | [#111](https://github.com/CuellarMarlon/timeblock/issues/111) |

## 8. Reparto inicial

Las tareas están asignadas en GitHub. En las issues compartidas, backend y frontend coordinan el contrato y sus respectivas partes. El reparto se puede ajustar con el equipo; la cantidad de issues no equivale por sí sola al esfuerzo y todavía faltan las estimaciones.

| Integrante | Área | Issues asignadas |
| --- | --- | --- |
| Marlon Cuellar (`CuellarMarlon`) | Backend | #83, #84, #85, #86, #89, #93, #94, #98, #110 y #107 |
| Raciel Pupo Santos (`P57749`) | Backend | #77, #88, #96, #100, #101, #102, #109, #111 y #107 |
| Ramón Hernández Camacho (`Ramonconda`) | Backend | #87, #90, #91, #95, #104, #105, #106 y #107 |
| Leandro Márquez (`Leandro-Marquez`) | Frontend | #87, #92, #99, #105 y #107 |
| Charly Blanco (`01Usuario`) | Frontend | #84, #93, #97, #103, #108 y #107 |

La integración y el cierre (#107) corresponden a los cinco. Las tareas conjuntas de cuentas (#84), aulas (#87), disponibilidad (#93) y reportes/PDF (#105) combinan backend y frontend. Para avanzar en paralelo, frontend puede empezar con contratos acordados y datos simulados mientras se implementan los endpoints.
