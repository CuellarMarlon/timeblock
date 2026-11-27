# Reglas de horarios para M6

Este documento recoge los acuerdos de la [issue #71](https://github.com/CuellarMarlon/timeblock/issues/71), revisados con la descripción del problema y el [diseño de la base de datos](../Diseño%20de%20la%20Base%20de%20Datos.pdf). Son reglas para implementar; no describen funcionalidades ya terminadas. Las opciones anteriores de P1–P14 quedan sustituidas por estos acuerdos.

El PDF define qué información y restricciones debe conservar el sistema, pero no especifica todas las decisiones del generador. Este documento concreta esas decisiones para Timeblock. Los permisos se detallan en [Identidad y permisos para M6](identidad-y-permisos-m6.md).

## 1. Qué es un horario

Un horario corresponde a un programa académico y comprende las clases de un semestre. El usuario podrá consultarlo por semana y, según la vista implementada, también por mes o por el semestre completo. La vista muestra principalmente la asignatura, el día, la hora y el aula; al abrir una clase muestra su bloque, tipo de actividad y complejidad.

La distribución semanal es fija: cada espacio mantiene la misma **asignatura, día, hora y aula** durante el semestre. El bloque de contenido puede cambiar entre semanas. Por ejemplo, Programación puede impartirse todos los lunes de 8:00 a 9:30 en la misma aula, con un bloque distinto cada semana.

Cada clase concreta es una **impartición**, con fecha, bloque, hora de inicio y fin, período y aula. Se almacena según `BLOQUE_EN_HORARIO` (§7.2.15 del diseño); la tabla semanal es una forma de presentarlas. Por su clave `(idHorario, idBloque, fecha)`, el mismo bloque no puede aparecer dos veces en el mismo horario y fecha, aunque se pretendan usar horas distintas.

## 2. Generación automática

### 2.1 Datos de entrada

El coordinador conectado debe tener una autorización vigente para generar horarios en el programa (RII01). Selecciona:

- El programa y las fechas de inicio y fin del semestre.
- Los días y franjas horarias semanales disponibles. El generador puede usar solo una parte de ellas.
- Los bloques candidatos asociados al programa, ordenados dentro de cada asignatura. Puede elegir bloques creados por otros coordinadores (RF15).
- La cantidad total de imparticiones, las proporciones de actividad, la cobertura mínima y el nivel de complejidad.

El período académico se construye con los meses y años de las fechas indicadas, en formato `MM-AAAA - MM-AAAA`: por ejemplo, `09-2026 - 01-2027`. Para ordenar o comparar períodos se interpretan sus componentes de año y mes, no se comparan directamente como texto. Cada impartición guarda ese mismo período como `periodoImp`. Las fechas concretas siguen siendo necesarias para generar las clases.

No se gestionan feriados ni otras fechas excluidas: la plantilla se repite durante el período indicado. No se clasifican las aulas por tipo; el generador elige aulas disponibles sin distinguir aulas de laboratorio. Cada espacio semanal conserva la misma aula durante todo el semestre.

### 2.2 Cantidad y proporciones

La cantidad solicitada es el número total de **imparticiones**, no de bloques distintos, y debe ser positiva. Un bloque programado en dos fechas cuenta dos veces. Por ejemplo, se pueden seleccionar 20 bloques candidatos y pedir 50 imparticiones.

Las proporciones de teoría, práctica y laboratorio se aplican a las imparticiones, cada una está entre 0 % y 100 % y suman 100 %. Para convertirlas en cantidades enteras:

1. Se calcula la cantidad ideal de cada tipo y se toma su parte entera.
2. Las imparticiones que falten se asignan a los tipos con mayor parte decimal.
3. En un empate se sigue el orden teoría, práctica y laboratorio.

Por ejemplo, para 7 imparticiones al 50 %, 40 % y 10 %, el cálculo da 3,5; 2,8; y 0,7. El resultado es **3 teóricas, 3 prácticas y 1 de laboratorio**. Una proporción positiva puede quedar con cero imparticiones; el resultado se muestra al coordinador.

En M4, `cantTotalBloques` se calcula como bloques distintos y no existe como columna de parámetros. Para M6 se acordó interpretar la cantidad solicitada como imparticiones y conservarla junto al horario. Esta adaptación debe reflejarse en la persistencia, los contratos y los reportes; no debe confundirse con el comportamiento actual de M4.

### 2.3 Cobertura y complejidad

El coordinador indica un único porcentaje mínimo de cobertura entre 0 % y 100 %, que se aplica por separado a cada asignatura representada entre los bloques seleccionados. Para cada asignatura:

> Bloques distintos que deben aparecer al menos una vez = redondeo hacia arriba de (bloques seleccionados de la asignatura × cobertura).

Con una cobertura del 80 %, si se seleccionaron 10 bloques de una asignatura deben aparecer al menos 8; si se seleccionaron 3 deben aparecer los 3. Con 0 % no se exige un mínimo para ninguna asignatura concreta: alguna puede quedar fuera, aunque las imparticiones solicitadas deben utilizar bloques candidatos.

La complejidad del horario es el promedio de las complejidades de sus **imparticiones**. Se asignan los valores bajo = 1, medio = 2 y alto = 3; un bloque repetido cuenta una vez por impartición. El promedio debe caer en el nivel solicitado:

| Nivel | Promedio |
| --- | --- |
| Bajo | Menor que 1,5 |
| Medio | Desde 1,5 hasta menos de 2,5 |
| Alto | Desde 2,5 |

### 2.4 Orden, distribución y aulas

El orden indicado por el coordinador es estricto dentro de cada asignatura: **todas las imparticiones de un bloque ocurren antes que cualquiera del siguiente bloque utilizado**. Un bloque candidato que no se utilice no altera el orden de los demás. El sistema decide automáticamente las repeticiones.

Entre las soluciones que cumplen las condiciones obligatorias, el generador prefiere:

1. Utilizar la mayor cantidad posible de bloques seleccionados.
2. Entre las opciones equivalentes, mantener el tipo de actividad de cada espacio semanal lo más estable posible.

El tipo puede cambiar entre semanas si es necesario para cumplir los criterios. Estas preferencias no sustituyen los requisitos obligatorios.

No pueden solaparse imparticiones de un mismo horario, aunque usen aulas diferentes. Tampoco puede haber imparticiones solapadas en la misma aula y fecha (RF17, RII04); los intervalos contiguos sí se permiten. La disponibilidad de un aula para un espacio semanal se comprueba en todas las fechas en que se usará.

### 2.5 Resultado y modificaciones

La cantidad de imparticiones debe encajar con las fechas y la plantilla semanal. Si no existe una solución que cumpla las condiciones, no se crea un horario incompleto: se informa el problema para que el coordinador ajuste los datos. Si una búsqueda se interrumpe o alcanza un límite técnico, no debe afirmarse que no existe solución sin haberlo comprobado.

El resultado muestra las clases por semana y un resumen de la cantidad, proporciones, cobertura por asignatura y promedio de complejidad. El horario se crea en `GENERADO`, conserva al coordinador y la autorización usados y guarda los parámetros utilizados (RF14).

Mientras esté en `GENERADO`, el creador puede corregir la programación. Las modificaciones deben seguir cumpliendo la plantilla semanal, los criterios, el orden y las reglas de aulas y solapamiento. Para comprobar el orden de los bloques ya programados se utiliza su orden en el horario vigente. Para incorporar otros bloques se vuelve a generar con una nueva selección y orden. Si cambia los criterios, el sistema vuelve a generar la programación del **mismo horario**; si no encuentra solución, conserva la última programación y los últimos criterios válidos. La actualización de criterios y programación debe ser atómica.

La lista completa de candidatos y su orden no se guardan: el esquema del PDF no describe dónde conservarlos. Cuando quiera volver a generar el horario, el coordinador vuelve a seleccionarlos y ordenarlos. Se conservan los parámetros y las imparticiones que exige el diseño. Como los candidatos no utilizados no pueden reconstruirse a partir del horario, queda por resolver en la implementación cómo comprobar la cobertura original al editar o enviar a validar sin conservar esa selección; no debe darse por verificada con datos insuficientes.

## 3. Validación y revisión

El flujo acordado usa los estados del PDF:

```text
GENERADO → PENDIENTE_VALIDACION → VALIDADO → EN_REVISION → APROBADO
                  │                               │
                  └→ GENERADO                     └→ REQUIERE_REGENERACION → RECHAZADO
```

El paso de `VALIDADO` a `EN_REVISION` es automático; se conserva el registro de validación. El horario `APROBADO` puede asignarse a estudiantes si cumple las demás condiciones.

- El coordinador creador, mientras atienda el programa, envía su horario a validar. Debe tener al menos un bloque y todas sus imparticiones completas, con fecha, hora de inicio y fin, período y aula; se comprueban las horas y los solapamientos.
- Si necesita correcciones antes de validarse, el creador puede devolverlo de `PENDIENTE_VALIDACION` a `GENERADO`. No se registra una validación negativa.
- Cualquier coordinador con autorización vigente y `puedeValidar` para el programa puede validar el horario, sea o no su creador (RF21, RII02). Se guarda la autorización concreta, la fecha y las observaciones.
- Una vez validado, la composición, programación y criterios quedan congelados. La validación y el envío automático a revisión deben conservar un estado y registro coherentes ante fallos o solicitudes concurrentes.
- Cualquier jefe de departamento puede revisar el horario: el diseño no asocia jefes a programas. Si lo aprueba, queda `APROBADO`. Si solicita otro, registra sus observaciones y criterios en la revisión y el horario queda `REQUIERE_REGENERACION` (RF18–RF20).
- El jefe escribe los criterios para la nueva generación en texto. El coordinador los interpreta y establece los parámetros del nuevo horario; se guardan tanto la revisión como los parámetros usados. El sistema no interpreta automáticamente el texto libre.
- Un coordinador con `puedeGenerar` vigente para el programa puede generar el nuevo horario, vinculado a la revisión. Cada revisión origina como máximo un horario nuevo. Si la generación tiene éxito, el original pasa a `RECHAZADO`; si falla, permanece en `REQUIERE_REGENERACION`. El nuevo comienza en `GENERADO` y sigue el mismo flujo.
- El creador puede cancelar su horario en `GENERADO` o `PENDIENTE_VALIDACION`. Se conserva en `CANCELADO` y no continúa el flujo. Los horarios validados, en revisión o aprobados no se cancelan mediante esta operación.
- Si el creador deja de atender el programa antes de enviar el horario a validar, no puede continuarlo hasta que vuelva a estar autorizado. Otro coordinador autorizado puede generar uno nuevo; no cambia el creador del anterior.

La vigencia de las autorizaciones se comprueba con la fecha UTC del servidor, como en M4. Sus extremos son inclusivos. Generar y validar son permisos independientes.

## 4. Asignación a estudiantes

- El coordinador que atiende el programa puede asignar un horario `APROBADO` a uno o varios estudiantes matriculados en ese programa (RF23). La asignación múltiple valida cada estudiante y muestra cuáles se realizaron y cuáles no, con su motivo.
- No se permiten asignaciones vigentes solapadas a horarios distintos del mismo programa y período académico (RF24, RII08). La operación debe protegerse también ante solicitudes concurrentes.
- El coordinador del programa puede cerrar una asignación sin borrarla. En un cambio aprobado se cierra la asignación anterior y se crea la nueva en una misma transacción (RII07).
- Antes de desmatricular a un estudiante deben cerrarse sus asignaciones activas en ese programa. Las asignaciones cerradas y la asistencia histórica se conservan.
- La capacidad de las aulas se almacena como exige el diseño, pero por ahora no se utiliza como límite de estudiantes asignados (§7.4).

## 5. Cambios respecto a M4

- En M6 solo se crearán horarios mediante generación automática. La creación manual de M4 dejará de ofrecerse y sus contratos e interfaz deberán actualizarse.
- Se acordó eliminar todos los horarios actuales de desarrollo o prueba y sus asociaciones de imparticiones al preparar la transición. Se conservan personas, programas, bloques y los demás datos reutilizables. Esta limpieza todavía no se ha ejecutado.
- La interpretación de `cantTotalBloques` cambia de bloques distintos en M4 a imparticiones solicitadas para M6. Los datos y respuestas existentes no deben reinterpretarse silenciosamente.
- Las tablas de revisión, validación y asignación, y las restricciones correspondientes, todavía deben implementarse. La existencia de un estado en el esquema no significa que el flujo ya funcione.

## 6. Casos para probar

1. Con 50 imparticiones y proporciones 50 %, 40 % y 10 %, se generan 25 teóricas, 20 prácticas y 5 de laboratorio; con 7 se obtiene 3, 3 y 1.
2. La cobertura del 80 % de 10 bloques exige 8 distintos; la de 3 exige 3. Con 0 % no hay mínimo por asignatura.
3. Los promedios 1,5 y 2,5 pertenecen respectivamente a los niveles medio y alto; las repeticiones cuentan para el promedio.
4. Cada espacio conserva asignatura, día, hora y aula durante el semestre. El bloque puede cambiar, respetando el orden estricto y la clave horario-bloque-fecha.
5. No se solapan clases del mismo horario ni imparticiones en una misma aula. Las franjas contiguas son válidas; se prueba también la concurrencia.
6. Si la cantidad no encaja con la plantilla, faltan aulas o los criterios son incompatibles, no se guarda una generación parcial. Una búsqueda interrumpida se distingue de la ausencia comprobada de solución.
7. Cambiar criterios regenera el mismo horario y, ante fallo, conserva la programación y los criterios válidos anteriores.
8. Solo se envía a validar un horario completo; un coordinador con `puedeValidar` vigente puede validar uno ajeno de su programa. Se conserva la autorización y pasa automáticamente a revisión.
9. La revisión aprobada permite asignar; la solicitud de regeneración conserva criterios y enlaza un único sustituto. Si falla la generación del sustituto, el original no cambia a `RECHAZADO`.
10. La asignación comprueba matrícula, estado y RII08, incluso bajo concurrencia. La desmatriculación con asignaciones activas se rechaza.

## 7. Trabajo para planificar en M6

Implementar la generación, programación y consultas; actualizar los contratos y la interfaz de M4; implementar validación, revisión y asignación; y comprobar las reglas con pruebas. [#69](https://github.com/CuellarMarlon/timeblock/issues/69) debe convertir este trabajo y las decisiones de [#67](identidad-y-permisos-m6.md) en tareas con dependencias y responsables.

El PDF establece `ESQUEMA_LLAVE` como relación física (§6.9.2 y §7.2.21). Debe conservarse conforme al diseño y completarse sus referencias al incorporar las relaciones faltantes; cómo se alimenta requiere análisis técnico antes de implementarlo. No se acordó eliminarla ni convertirla en relación puramente teórica.
