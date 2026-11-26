# Bajas y eliminación en M4

Acuerdos para la [issue #66](https://github.com/CuellarMarlon/timeblock/issues/66), a partir del [diseño de la base de datos](../Diseño%20de%20la%20Base%20de%20Datos.pdf) y del modelo actual de M4. Estas reglas deben implementarse y probarse como parte de la issue.

## Matrículas

Se podrá desmatricular a un estudiante de un programa eliminando la relación entre ambos. No se elimina al estudiante ni al programa y se permitirá matricular de nuevo a la misma pareja.

El diseño pide registrar y consultar matrículas (RF07 y RI07), pero no exige expresamente conservar su historial. La relación `MATRICULA` (§7.2.8) no tiene fechas ni estado. Por tanto, en M4 no se conservará cuándo se realizó una baja o una nueva matrícula. El historial de RI21 corresponde a las asignaciones de horarios, no a las matrículas.

Cuando se implementen las asignaciones de horarios en M6, habrá que revisar cómo afecta la desmatriculación a las asignaciones y a su historial.

## Horarios

Los horarios se conservan con sus bloques y fechas, incluso cuando se hayan creado por error. M4 no ofrecerá una operación para eliminarlos.

La cancelación se decidirá e implementará más adelante, junto con el ciclo de vida de los horarios. El diseño incluye el estado `CANCELADO`, pero todavía deben acordarse sus efectos y las condiciones para usarlo.

## Personas, programas, asignaturas y bloques

- Se permitirá eliminar físicamente un registro que no tenga dependencias. En el caso de una persona, se eliminarán juntos la persona y su perfil, en una transacción.
- Si tiene dependencias, no se podrá eliminar físicamente. Se podrá marcar como **inactivo** cuando ya no deba utilizarse.
- Un registro inactivo podrá **reactivarse**.
- Las relaciones existentes se conservarán. Los registros inactivos seguirán apareciendo identificados como tales cuando se consulten datos en los que ya participan.
- Los registros inactivos no estarán disponibles para nuevas operaciones o relaciones: por ejemplo, no se matriculará a un estudiante inactivo, no se creará un horario para un programa o coordinador inactivo y no se incluirá un bloque inactivo en un horario nuevo. Tampoco se asociará una asignatura inactiva a un nuevo programa ni se utilizará para crear bloques nuevos.

Inactivar un registro no elimina ni modifica automáticamente sus relaciones. La interfaz debe indicar cuándo una eliminación está impedida por dependencias y ofrecer la baja cuando corresponda. La API también debe comprobar el estado y las dependencias, sin depender únicamente de la interfaz.

## Asociaciones

- **Programa y asignatura:** se podrá quitar la asociación si no quedan bloques de esa asignatura asociados al programa. Esto respeta RII03 (§7.3).
- **Bloque y programa:** se podrá quitar la asociación si el bloque no está incluido en un horario existente de ese programa. Si se utiliza en alguno, la operación se rechaza para conservar la compatibilidad de los horarios.
- Quitar una asociación no elimina los registros que relaciona.

## Autorizaciones

Una autorización creada por error podrá eliminarse si ningún horario la utiliza. Si ya fue utilizada, se conservará y se podrá cerrar su vigencia sin invalidar los horarios que se generaron durante ella. Cerrar la autorización no elimina horarios ni altera sus referencias.

El diseño exige que el horario conserve la referencia a la autorización utilizada (§7.2.14) y que esta estuviera vigente y permitiera generar en la fecha correspondiente (RII01). Al modificar la vigencia o los permisos debe evitarse contradecir esos usos existentes. El modelo actual no conserva un historial de cambios de los permisos; un historial completo requiere un diseño adicional.

## Comportamiento de las operaciones

La interfaz pedirá confirmación antes de eliminar o desvincular y mostrará un mensaje comprensible si la operación está impedida. Las operaciones que deban modificar varios registros se realizarán en una transacción.

La API responderá `204` cuando complete una eliminación o desvinculación, `404` si el recurso o relación no existe, `409` si hay una dependencia o regla que impide la operación y `400` ante una solicitud inválida. La implementación debe incluir pruebas de los casos permitidos y rechazados, la reactivación y la consistencia de los cambios.
