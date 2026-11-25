# Reglas de negocio para generación, revisión y asignación de horarios (M6)

Propuesta para la issue #71. Define cómo se interpretan los parámetros de generación, qué ocurre cuando no hay solución, el ciclo de estados del horario y las condiciones para asignarlo a estudiantes. Cada regla tiene un identificador para citarla en las issues y pruebas de M6.

**Estado:** propuesta pendiente de acuerdo del equipo. Las reglas marcadas con una decisión (P1–P14) aplican la opción recomendada; si el equipo elige otra, se corrige la regla antes de implementar.

| Dato          | Valor                                                                                                   |
| ------------- | ------------------------------------------------------------------------------------------------------- |
| Fecha         | 2026-11-25                                                                                              |
| Base          | `develop` en `e847c99`                                                                                  |
| Referencias   | RF12–RF24, RI13, RI17–RI21, RII01, RII02 y RII08 del PDF de diseño; secciones 7.1, 7.2.14–7.2.19 y 7.4  |
| Relacionado   | `docs/revisiones/cobertura-diseno-m5.md` (D2–D5, D8, D9), `docs/api/contrato-api.md` (decisiones 5 y 6) |
| Bloques de M6 | B (generación), C (revisión y validación) y D (asignación) de la revisión de cobertura                  |

## 1. Punto de partida

Lo que ya existe en `develop` y condiciona las reglas:

- HORARIO guarda `nivelComplejidadHorario`, `propTeorica`, `propPractica`, `propLaboratorio` y `coberturaAsignatura`, todos opcionales. Las proporciones tienen CHECK (entre 0 y 1 y suma 1, o las tres nulas) y la cobertura, CHECK `>= 0`. El nivel no tiene CHECK de dominio (D3).
- `cantTotalBloques` no es una columna: la API cuenta los bloques distintos del horario (D2, decisión 6 del contrato).
- `idRevisionOrigen` existe sin clave foránea, porque REVISION no existe (D5).
- El CHECK de `estadoHorario` admite los ocho estados del diseño, pero el dominio solo usa `GENERADO`.
- RII01 se comprueba con la fecha UTC del servidor y la autorización vigente de inicio más reciente.
- `periodoAcademico` es texto libre y el filtro compara el texto exacto. El ejemplo del contrato es `2026-2027`.

## 2. Generación automática (RF12–RF15, RI13)

### 2.1 Entrada

| Id  | Regla                                                                                                                                                                                                                                                                                        |
| --- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| G01 | La solicitud indica programa, coordinador, período académico, parámetros (nivel, tres proporciones, cobertura y cantidad de bloques) y, opcionalmente, los bloques candidatos. Todos los parámetros son obligatorios.                                                                        |
| G02 | Los candidatos son los bloques indicados o, si no se indica ninguno, todos los bloques asociados al programa (BLOQUE_PROGRAMA). Cada candidato debe poder usarse en el programa (RF15, RII03); si alguno no puede, la solicitud se rechaza entera. Los candidatos repetidos cuentan una vez. |
| G03 | RII01 se comprueba como en M4: autorización vigente con `puedeGenerar` en la fecha de generación. La comprobación bloquea la autorización para evitar la carrera descrita en D8.                                                                                                             |
| G04 | La generación también necesita las fechas de impartición (P8): una lista no vacía de fechas distintas.                                                                                                                                                                                       |

### 2.2 Interpretación de los parámetros

| Id  | Parámetro                      | Regla                                                                                                                                                                                                                                                                                                                                               |
| --- | ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| G05 | `cantTotalBloques` (N)         | Número de **bloques distintos** que debe tener el horario. Entero mayor que 0. Se guarda como columna (P7).                                                                                                                                                                                                                                         |
| G06 | Proporciones                   | Fracción de los N bloques de cada tipo de actividad. Se guardan con hasta 4 decimales, entre 0 y 1, y deben sumar exactamente 1 (CHECK actual).                                                                                                                                                                                                     |
| G07 | Reparto entero de proporciones | Las cantidades por tipo se calculan por **restos mayores**: se toma la parte entera de `N × p` de cada tipo y las unidades que faltan hasta N se dan, una a una, a los tipos con mayor parte decimal. Los empates se resuelven en el orden TEORICA, PRACTICA, LABORATORIO. La suma siempre es N. Una proporción positiva puede quedar en 0 bloques. |
| G08 | `coberturaAsignatura` (c)      | Fracción mínima de las asignaturas del programa (PROGRAMA_ASIGNATURA, total S) que deben tener al menos un bloque en el horario. Valor entre 0 y 1 (se añade el CHECK `<= 1`). Se exigen `k = ⌈c × S⌉` asignaturas, calculado con aritmética decimal exacta (P5).                                                                                   |
| G09 | `nivelComplejidadHorario`      | Dominio BAJO, MEDIO, ALTO (se añade el CHECK). Se interpreta como la **media** de complejidad de los bloques elegidos, con BAJO = 1, MEDIO = 2 y ALTO = 3: BAJO si la media es menor que 1,5; MEDIO si está entre 1,5 (incluido) y 2,5 (excluido); ALTO si es 2,5 o más (P6).                                                                       |
| G10 | Respuesta                      | La respuesta devuelve, además del horario, las cantidades por tipo de G07, el valor k de G08 y la media de G09, para que el cliente vea cómo se interpretaron los parámetros.                                                                                                                                                                       |

### 2.3 Resultado

| Id  | Regla                                                                                                                                                                                                                                                                                         |
| --- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| G11 | El horario generado tiene exactamente N bloques distintos que cumplen las cantidades por tipo (G07), cubren al menos k asignaturas (G08) y tienen la media en la banda del nivel pedido (G09).                                                                                                |
| G12 | Cada bloque elegido tiene una sola impartición. Los bloques se ordenan por complejidad ascendente, después por nombre de asignatura y por identificador; el bloque en la posición `i` (desde 0) se asigna a la fecha `⌊i × F / N⌋` de la lista ordenada de F fechas. Si F > N, sobran fechas. |
| G13 | La hora, el período y el aula de cada impartición no los decide la generación: se programan después (RF16, bloque A), mientras el horario está en `GENERADO`.                                                                                                                                 |
| G14 | La generación es determinista: con la misma solicitud y los mismos datos devuelve los mismos bloques. Las pruebas comprueban las propiedades de G11, no un conjunto concreto, salvo cuando la solución sea única.                                                                             |
| G15 | La búsqueda debe ser completa: si existe una combinación que cumple G11, la encuentra. Con los volúmenes del proyecto basta una búsqueda con retroceso y poda.                                                                                                                                |
| G16 | El horario se crea con estado `GENERADO`, los parámetros guardados y, si viene de una revisión, `idRevisionOrigen` (sección 3.4).                                                                                                                                                             |
| G17 | Un horario generado no admite cambios en su composición (bloques y fechas), para que los parámetros guardados sigan describiéndolo (RF14, RF35). Solo se programan sus imparticiones. Para cambiar la composición se genera otro horario.                                                     |
| G18 | El alta manual de M4 se mantiene: deja los parámetros nulos y admite editar la composición mientras está en `GENERADO`. Un horario es automático si y solo si tiene parámetros (P13). Se añade un CHECK: o todos los parámetros son nulos o ninguno lo es.                                    |

### 2.4 Combinaciones imposibles y ausencia de solución

Antes de buscar, se comprueban condiciones necesarias. Si falla alguna, la respuesta es `409` con un código por causa y **no se guarda nada**.

| Id  | Comprobación previa                                                                                                                                                                                                                                                                                   | Código propuesto                      |
| --- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------- |
| G19 | N no supera el número de candidatos.                                                                                                                                                                                                                                                                  | `GENERACION_CANDIDATOS_INSUFICIENTES` |
| G20 | La cantidad de cada tipo (G07) no supera los candidatos de ese tipo.                                                                                                                                                                                                                                  | `GENERACION_TIPO_INSUFICIENTE`        |
| G21 | k no supera N ni el número de asignaturas con algún candidato.                                                                                                                                                                                                                                        | `GENERACION_COBERTURA_IMPOSIBLE`      |
| G22 | La banda del nivel es alcanzable: con las cantidades de G07, el intervalo entre la media mínima posible (los candidatos más simples de cada tipo) y la máxima posible (los más complejos) toca la banda del nivel pedido. Es una condición necesaria, no suficiente: no tiene en cuenta la cobertura. | `GENERACION_NIVEL_IMPOSIBLE`          |

| Id  | Regla                                                                                                                                                                                                                         |
| --- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| G23 | Si las comprobaciones previas pasan pero la búsqueda no encuentra combinación, la respuesta es `409` con el código `GENERACION_SIN_SOLUCION` y la lista de criterios que no pudieron cumplirse juntos. No se crea el horario. |
| G24 | Los intentos fallidos no se guardan: el diseño no tiene un estado para ellos y RF35 solo lista horarios generados.                                                                                                            |
| G25 | Orden de validación, igual que en M4: formato (`400`), existencia de programa, coordinador y bloques (`404`), autorización (`403`), reglas de composición y combinaciones imposibles (`409`).                                 |

### 2.5 Ejemplos

Programa P con tres asignaturas (A1, A2, A3) y estos candidatos:

| Bloque | Asignatura | Tipo        | Nivel |
| ------ | ---------- | ----------- | ----- |
| b1     | A1         | TEORICA     | BAJO  |
| b2     | A1         | TEORICA     | MEDIO |
| b3     | A1         | PRACTICA    | ALTO  |
| b4     | A2         | TEORICA     | MEDIO |
| b5     | A2         | PRACTICA    | BAJO  |
| b6     | A2         | LABORATORIO | ALTO  |
| b7     | A3         | TEORICA     | ALTO  |
| b8     | A3         | PRACTICA    | MEDIO |
| b9     | A3         | LABORATORIO | BAJO  |
| b10    | A1         | LABORATORIO | MEDIO |

| Caso | Parámetros                                                  | Resultado esperado                                                                                                                                                                                 |
| ---- | ----------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| E01  | N = 6; T 0,5, P 0,3, L 0,2; c = 1; MEDIO; fechas d1, d2, d3 | Cantidades: 3,0 / 1,8 / 1,2 → 3, 1, 1 y la unidad que falta va a P (0,8): **T3 P2 L1**. k = 3. Una solución válida: b1, b2, b4, b5, b8, b6; media 11/6 ≈ 1,83, MEDIO. Dos bloques por fecha (G12). |
| E02  | N = 5; T 0,3333, P 0,3333, L 0,3334                         | 1,6665 / 1,6665 / 1,667 → 1, 1, 1; las dos unidades van a L (0,667) y a T (empata con P y va primero): **T2 P1 L2**.                                                                               |
| E03  | N = 4; T 0,6, P 0,3, L 0,1                                  | 2,4 / 1,2 / 0,4 → 2, 1, 0; la unidad que falta empata entre T y L (0,4) y va a T: **T3 P1 L0**. Válido: la respuesta informa que L quedó en 0.                                                     |
| E04  | N = 8; T 0,25, P 0,25, L 0,5                                | L necesita 4 bloques y solo hay 3 candidatos: `409 GENERACION_TIPO_INSUFICIENTE`.                                                                                                                  |
| E05  | N = 2; c = 1                                                | k = 3 > N: `409 GENERACION_COBERTURA_IMPOSIBLE`.                                                                                                                                                   |
| E06  | N = 3; T 1, P 0, L 0; c = 0; BAJO                           | Los tres teóricos más simples (b1, b2, b4) dan media 5/3 ≈ 1,67, ya fuera de BAJO: `409 GENERACION_NIVEL_IMPOSIBLE`.                                                                               |
| E07  | c = 0,67 con S = 3                                          | 0,67 × 3 = 2,01 → k = 3, porque 2/3 no llega al 67 %. Para pedir dos de tres asignaturas hay que enviar un valor de hasta 0,6666. El cliente muestra k antes de generar.                           |
| E08  | N = 2; fechas d1, d2, d3                                    | `⌊0 × 3/2⌋ = 0` y `⌊1 × 3/2⌋ = 1`: los bloques van a d1 y d2; d3 queda sin usar.                                                                                                                   |

## 3. Revisión y validación (RF18–RF22, RI17–RI20, RII02)

### 3.1 Ciclo de estados

Orden propuesto: **primero revisa el jefe de departamento y después valida un coordinador autorizado** (P1). Sigue el orden del dominio de la sección 7.1 y de los RF18–RF21.

```text
             enviar a revisión            revisión APROBADO           validación
GENERADO ───────────────────► EN_REVISION ─────────────────► APROBADO ──────────► VALIDADO ──► asignable
    │                              │
    │ cancelar                     │ revisión SOLICITAR_NUEVA_GENERACION
    ▼                              ▼
CANCELADO ◄────────────── REQUIERE_REGENERACION ──────────────► RECHAZADO
                 cancelar                     se genera el horario sustituto
```

| Id  | Transición                                         | Quién                                                                                                 | Condiciones                                                                                                               | Efecto                                                                                                      |
| --- | -------------------------------------------------- | ----------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| C01 | `GENERADO` → `EN_REVISION`                         | Coordinador que generó el horario                                                                     | Al menos un bloque; todas las imparticiones completas (hora, período y aula, D4) y sin solapamientos de aula (RII04, D9). | La composición y la programación quedan congeladas.                                                         |
| C02 | `EN_REVISION` → `APROBADO`                         | Jefe de departamento                                                                                  | Ninguna adicional.                                                                                                        | Se crea REVISION con `decision = APROBADO` y `criteriosNuevaGeneracion` nulo.                               |
| C03 | `EN_REVISION` → `REQUIERE_REGENERACION`            | Jefe de departamento                                                                                  | `criteriosNuevaGeneracion` no vacío.                                                                                      | Se crea REVISION con `decision = SOLICITAR_NUEVA_GENERACION`.                                               |
| C04 | `APROBADO` → `VALIDADO`                            | Coordinador con `puedeValidar` vigente en la fecha de validación para el programa del horario (RII02) | La autorización se resuelve como en RII01 (vigente, inicio más reciente) y se bloquea al comprobarla (D8).                | Se crea VALIDACION con la autorización usada (`idCoordinadorVal`, `fechaInicioVal`), fecha y observaciones. |
| C05 | `REQUIERE_REGENERACION` → `RECHAZADO`              | Automático                                                                                            | Se genera un horario con `idRevisionOrigen` igual a la revisión (sección 3.4).                                            | Ocurre en la misma transacción que la creación del horario sustituto.                                       |
| C06 | `GENERADO` o `REQUIERE_REGENERACION` → `CANCELADO` | Coordinador que generó el horario                                                                     | Ninguna adicional.                                                                                                        | El horario se conserva para el historial.                                                                   |

### 3.2 Invariantes

| Id  | Regla                                                                                                                                                                                                                                                   |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| C07 | Solo se admiten las transiciones de la tabla anterior. Cualquier otra responde `409` con el estado actual. `VALIDADO`, `RECHAZADO` y `CANCELADO` no tienen salida en M6.                                                                                |
| C08 | Cada horario tiene como máximo una REVISION y una VALIDACION, porque el ciclo no vuelve a `EN_REVISION` ni a `APROBADO`. Se protege con índices únicos sobre `idHorario` en ambas tablas.                                                               |
| C09 | Fechas: `fechaRevision >= fechaCreacion` y `fechaValidacion >= fechaRevision`. Las asigna el servidor, con la misma precisión que `fechaCreacion`.                                                                                                      |
| C10 | El estado y el registro (REVISION o VALIDACION) se escriben en la misma transacción. El cambio de estado comprueba el estado esperado para que dos revisiones simultáneas no se acepten ambas (concurrencia optimista o `FOR UPDATE` sobre el horario). |
| C11 | Los horarios manuales siguen el mismo ciclo.                                                                                                                                                                                                            |
| C12 | Cualquier jefe de departamento puede revisar cualquier horario: el diseño no relaciona jefes y programas (P14). Los permisos efectivos dependen de #67.                                                                                                 |
| C13 | Puede validar el mismo coordinador que generó el horario si tiene `puedeValidar`: el diseño separa los permisos, no las personas (P4).                                                                                                                  |
| C14 | No hay validaciones negativas: VALIDACION no tiene un campo de resultado. Si un coordinador no está de acuerdo, no valida y lo comunica fuera del sistema (P3).                                                                                         |
| C15 | `PENDIENTE_VALIDACION` no se usa: `APROBADO` ya significa "aprobado y pendiente de validar" (P2).                                                                                                                                                       |

### 3.3 Visibilidad de estados (RI20)

| Estado                  | Significado                                                 | Editable                      |
| ----------------------- | ----------------------------------------------------------- | ----------------------------- |
| `GENERADO`              | Constituido; se programa y, si es manual, se compone.       | Sí (G17 para los automáticos) |
| `EN_REVISION`           | Enviado al jefe de departamento.                            | No                            |
| `APROBADO`              | Aprobado por el jefe, pendiente de validación.              | No                            |
| `REQUIERE_REGENERACION` | El jefe pidió otro horario y aún no se ha generado.         | No                            |
| `VALIDADO`              | Validado por un coordinador autorizado. Horario final.      | No                            |
| `RECHAZADO`             | Sustituido por el horario generado a partir de su revisión. | No                            |
| `CANCELADO`             | Retirado por su coordinador.                                | No                            |

### 3.4 Regeneración (RF19, RF20, RI18)

| Id  | Regla                                                                                                                                                                   |
| --- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| C16 | Solo puede generarse un horario con `idRevisionOrigen = R` si R tiene `decision = SOLICITAR_NUEVA_GENERACION` y su horario está en `REQUIERE_REGENERACION`.             |
| C17 | El horario sustituto pertenece al mismo programa y período que el original. Puede generarlo cualquier coordinador con `puedeGenerar` vigente en ese programa (RII01).   |
| C18 | Cada revisión origina como máximo un horario: índice único sobre `idRevisionOrigen` y clave foránea a REVISION (D5).                                                    |
| C19 | `criteriosNuevaGeneracion` es texto libre para quien genera. Los parámetros que se usen realmente se guardan en el horario sustituto, que es lo que se puede verificar. |

## 4. Asignación a estudiantes (RF23, RF24, RI21, RII08)

| Id  | Regla                                                                                                                                                                                                                                                                                                                                                                                |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| A01 | Solo se asignan horarios en estado `VALIDADO`. RF23 habla de horarios aprobados; con el orden de P1, el horario final es el validado.                                                                                                                                                                                                                                                |
| A02 | El estudiante debe estar matriculado en el programa del horario (RF23).                                                                                                                                                                                                                                                                                                              |
| A03 | Asigna el coordinador del programa, de forma individual o para una lista de estudiantes (P12). En una asignación por lista, cada estudiante se valida por separado y la respuesta informa qué asignaciones se crearon y cuáles no, con su motivo.                                                                                                                                    |
| A04 | `fechaAsignacion` la pone el servidor, con precisión de microsegundos. `fechaFinAsignacion` queda nula mientras la asignación está abierta.                                                                                                                                                                                                                                          |
| A05 | La vigencia de una asignación es el intervalo semiabierto `[fechaAsignacion, fechaFinAsignacion)`, abierto por la derecha si no hay fin. Así, cerrar una asignación y abrir otra en el mismo instante no se considera solapamiento (RII07).                                                                                                                                          |
| A06 | RF24 y RII08: un estudiante no puede tener dos asignaciones con vigencias solapadas a horarios del mismo programa y período académico. La regla también impide asignar dos veces el mismo horario a la vez. Se implementa con un disparador o una validación transaccional que bloquea la fila del estudiante (`FOR UPDATE`), como en #54, para serializar asignaciones simultáneas. |
| A07 | El período académico se compara normalizado (sin espacios en los extremos) y con formato `AAAA-AAAA`, como el ejemplo del contrato (P11).                                                                                                                                                                                                                                            |
| A08 | Cerrar una asignación pone `fechaFinAsignacion` con la hora del servidor; no se borra. Una vez cerrada, el mismo horario puede volver a asignarse con otra `fechaAsignacion` (F13).                                                                                                                                                                                                  |
| A09 | `VALIDADO` no tiene salida en M6 (C07). Si más adelante se permite retirar un horario validado, antes habrá que cerrar sus asignaciones abiertas.                                                                                                                                                                                                                                    |
| A10 | Qué ocurre con las asignaciones si se da de baja la matrícula queda para #66.                                                                                                                                                                                                                                                                                                        |

### 4.1 Disponibilidad de un horario

Un horario H está **disponible** para el estudiante E en el instante t si se cumplen todas estas condiciones. La API de asignación responde `409` con la primera que falle.

| Id  | Condición                                                                                                                                                    | Código propuesto            |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------- |
| A11 | H está en `VALIDADO`.                                                                                                                                        | `HORARIO_NO_ASIGNABLE`      |
| A12 | E está matriculado en el programa de H.                                                                                                                      | `ESTUDIANTE_NO_MATRICULADO` |
| A13 | E no tiene una asignación abierta a otro horario, o al mismo, del mismo programa y período (A06).                                                            | `ASIGNACION_SOLAPADA`       |
| A14 | Queda cupo: las asignaciones abiertas de H son menos que el cupo, que es la menor capacidad de las aulas de sus imparticiones (sección 7.4 del diseño; P10). | `HORARIO_SIN_CUPO`          |

La ocupación de un horario para RF27 es `asignaciones abiertas / cupo`.

### 4.2 Ejemplos

Estudiante E matriculado en P; H1 y H2 validados, de P y período `2026-2027`; H3 validado, de P y período `2027-2028`; cupo de H1 = 30.

| Caso | Situación                                                                    | Resultado                                                 |
| ---- | ---------------------------------------------------------------------------- | --------------------------------------------------------- |
| E09  | E tiene una asignación abierta a H1 desde `2026-09-01T10:00Z`. Se asigna H2. | `409 ASIGNACION_SOLAPADA`.                                |
| E10  | Se cierra la asignación a H1 en t y se asigna H2 en t.                       | Se acepta: `[.., t)` y `[t, ..)` no se solapan.           |
| E11  | Con H1 abierta, se asigna H3.                                                | Se acepta: es otro período.                               |
| E12  | Con H1 abierta, se vuelve a asignar H1.                                      | `409 ASIGNACION_SOLAPADA`.                                |
| E13  | H1 tiene 30 asignaciones abiertas.                                           | `409 HORARIO_SIN_CUPO`.                                   |
| E14  | Dos peticiones simultáneas asignan H1 y H2 a E, sin asignaciones previas.    | Se acepta una y la otra recibe `409 ASIGNACION_SOLAPADA`. |
| E15  | Se asigna un horario en `APROBADO`.                                          | `409 HORARIO_NO_ASIGNABLE`.                               |

## 5. Fechas y zona horaria

| Id  | Regla                                                                                                                                                                                                                                                                                                                                                                                       |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| F01 | Las reglas que comparan una fecha con una vigencia (RII01, RII02) usan la fecha en la zona horaria de la institución, configurable y por defecto `America/Havana` (P9). Hoy M4 usa la fecha UTC: entre las 19:00 y las 24:00 de Cuba (20:00 en horario de verano) la fecha UTC ya es la del día siguiente, así que una autorización que vence "hoy" deja de valer antes de terminar el día. |
| F02 | Las marcas de tiempo se siguen guardando y devolviendo en UTC con precisión de microsegundos, como en M4.                                                                                                                                                                                                                                                                                   |

## 6. Decisiones que debe tomar el equipo

| Id  | Decisión                                         | Opción recomendada (aplicada en este documento)                               | Alternativa                                                                         | Reglas afectadas |
| --- | ------------------------------------------------ | ----------------------------------------------------------------------------- | ----------------------------------------------------------------------------------- | ---------------- |
| P1  | Orden de revisión y validación                   | Primero el jefe revisa y después un coordinador valida.                       | Validar primero y que el jefe apruebe al final (el horario final sería `APROBADO`). | C01–C05, A01     |
| P2  | Uso de `PENDIENTE_VALIDACION`                    | No usarlo y retirarlo del CHECK.                                              | Mantenerlo como paso intermedio entre `APROBADO` y `VALIDADO`.                      | C15              |
| P3  | Validaciones negativas                           | No existen.                                                                   | Añadir un resultado a VALIDACION y usar `RECHAZADO` para ellas.                     | C14, C05         |
| P4  | ¿Puede validar quien generó?                     | Sí, si tiene `puedeValidar`.                                                  | Exigir otro coordinador (puede bloquear programas con un solo coordinador).         | C13              |
| P5  | Significado de la cobertura                      | Fracción de asignaturas del programa, con `k = ⌈c × S⌉`.                      | Número mínimo de asignaturas.                                                       | G08, E07         |
| P6  | Significado del nivel de complejidad             | Banda de la media de complejidad.                                             | Nivel máximo permitido, o todos los bloques del mismo nivel.                        | G09, G22         |
| P7  | `cantTotalBloques` (D2)                          | Columna con los bloques distintos pedidos.                                    | Dato calculado, como en M4.                                                         | G05              |
| P8  | De dónde salen las fechas de un horario generado | Lista de fechas en la solicitud y reparto de G12.                             | Rango de fechas y días de la semana.                                                | G04, G12         |
| P9  | Zona horaria de las reglas por fecha             | Zona de la institución, configurable; también corrige RII01 de M4.            | Mantener UTC y documentarlo.                                                        | F01              |
| P10 | Cupo del horario por capacidad de aula           | Aplicarlo (A14).                                                              | No limitar asignaciones.                                                            | A14              |
| P11 | Formato de `periodoAcademico`                    | `AAAA-AAAA` con CHECK.                                                        | Texto libre normalizado.                                                            | A07              |
| P12 | Quién asigna horarios                            | El coordinador del programa, de forma explícita.                              | Asignación automática al validar.                                                   | A03              |
| P13 | Distinguir horarios manuales y automáticos       | Por los parámetros: nulos en los manuales. Cambia la decisión 5 del contrato. | Columna de origen explícita.                                                        | G18, RF35        |
| P14 | Relación entre jefes y programas                 | Cualquier jefe revisa cualquier horario.                                      | Asociar jefes a programas (cambia el diseño).                                       | C12              |

## 7. Cambios de esquema que se derivan

Aplicando las opciones recomendadas:

- HORARIO: columna `cantTotalBloques` (entero, `> 0` o nula), CHECK de dominio en `nivelComplejidadHorario`, CHECK `coberturaAsignatura <= 1`, CHECK de "todos los parámetros o ninguno", índice único y clave foránea en `idRevisionOrigen`. Retirar `PENDIENTE_VALIDACION` del CHECK de estado (P2).
- REVISION y VALIDACION: tablas nuevas según 7.2.18 y 7.2.19, con índice único sobre `idHorario` (C08). VALIDACION con clave foránea compuesta a COORDINADOR_EN_PROGRAMA.
- HORARIO_ESTUDIANTE: tabla nueva según 7.2.16, con el disparador o la validación de A06.
- `periodoAcademico`: CHECK de formato (P11). Antes hay que revisar los datos existentes.
- Contrato: actualizar las decisiones 5 y 6 y documentar los códigos de error de las secciones 2.4 y 4.1.

## 8. Tareas de M6 que implementan estas reglas

Las issues se crearán al planificar M6 en #69. Correspondencia propuesta con los bloques de la revisión de cobertura:

| Bloque                      | Reglas                       | Depende de                         |
| --------------------------- | ---------------------------- | ---------------------------------- |
| A. Imparticiones y aulas    | Precondición de C01          | D4, D9                             |
| B. Generación automática    | G01–G25, P5–P8, P13          | A para enviar a revisión           |
| C. Revisión y validación    | C01–C19, F01, P1–P4, P9, P14 | #67 para los permisos              |
| D. Asignación a estudiantes | A01–A14, P10–P12             | C (solo se asignan validados), #66 |
