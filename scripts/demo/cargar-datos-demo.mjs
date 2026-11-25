#!/usr/bin/env node
// Carga los datos de la demostración de M4 a través de la API y comprueba que
// los casos inválidos se rechazan. Se puede ejecutar varias veces: reutiliza lo
// que ya existe en lugar de duplicarlo.
//
// Uso (con la API en marcha):
//   node scripts/demo/cargar-datos-demo.mjs [--api http://localhost:5131] [--sin-rechazos]

const args = process.argv.slice(2);
const API = (valorDe("--api") ?? "http://localhost:5131").replace(/\/+$/, "");
const COMPROBAR_RECHAZOS = !args.includes("--sin-rechazos");
const PERIODO = "2026-2027";

function valorDe(opcion) {
  const i = args.indexOf(opcion);
  return i >= 0 ? args[i + 1] : undefined;
}

// ---------------------------------------------------------------------------
// Datos de la demostración
// ---------------------------------------------------------------------------

const JEFE = { nombre: "María Rodríguez Díaz", perfil: { tipo: "jefeDepartamento" } };

const COORDINADORES = {
  luis: { nombre: "Luis García Pérez", especialidad: "Bases de datos" },
  elena: { nombre: "Elena Martín Suárez", especialidad: "Programación" },
};

const PROGRAMAS = {
  informatica: { nombre: "Ingeniería Informática", planEstudio: "Plan 2024" },
  computacion: { nombre: "Ciencia de la Computación", planEstudio: "Plan 2023" },
};

const ASIGNATURAS = {
  bd2: "Bases de Datos II",
  declarativa: "Programación Declarativa",
  estadistica: "Estadística",
  optimizacion: "Optimización",
};

// Programa -> asignaturas del plan.
const PLANES = {
  informatica: ["bd2", "estadistica", "optimizacion"],
  computacion: ["bd2", "declarativa", "estadistica"],
};

// Luis genera y valida en Informática, pero en Computación solo valida: sirve
// para mostrar el rechazo 403 al crear un horario sin permiso de generación.
const AUTORIZACIONES = [
  {
    coordinador: "luis",
    programa: "informatica",
    fechaInicio: "2026-09-01",
    fechaFin: null,
    puedeGenerar: true,
    puedeValidar: true,
  },
  {
    coordinador: "luis",
    programa: "computacion",
    fechaInicio: "2026-09-01",
    fechaFin: null,
    puedeGenerar: false,
    puedeValidar: true,
  },
  {
    coordinador: "elena",
    programa: "computacion",
    fechaInicio: "2026-09-01",
    fechaFin: "2027-07-31",
    puedeGenerar: true,
    puedeValidar: false,
  },
];

const ESTUDIANTES = [
  {
    nombre: "Ana Pérez López",
    edad: 20,
    grupo: "C311",
    coordinador: "luis",
    programas: ["informatica"],
  },
  {
    nombre: "Carlos Gómez Ruiz",
    edad: 21,
    grupo: "C311",
    coordinador: "luis",
    programas: ["informatica"],
  },
  {
    nombre: "Daniela Torres Vega",
    edad: 20,
    grupo: "C312",
    coordinador: "luis",
    programas: ["informatica", "computacion"],
  },
  {
    nombre: "Ernesto Blanco Ríos",
    edad: 22,
    grupo: "C312",
    coordinador: "elena",
    programas: ["computacion"],
  },
  {
    nombre: "Fernanda Castro Gil",
    edad: 19,
    grupo: "C311",
    coordinador: "elena",
    programas: ["computacion"],
  },
  {
    nombre: "Gabriel Ortiz Mena",
    edad: 21,
    grupo: "C312",
    coordinador: "elena",
    programas: ["computacion"],
  },
];

// El primer bloque se comparte entre los dos programas (RF15).
const BLOQUES = {
  bd2Intro: {
    descripcion: "Modelo relacional y álgebra relacional",
    asignatura: "bd2",
    tipo: "teoria",
    nivel: "bajo",
    coordinador: "luis",
    programas: ["informatica", "computacion"],
  },
  bd2Normal: {
    descripcion: "Dependencias funcionales y normalización",
    asignatura: "bd2",
    tipo: "teoria",
    nivel: "medio",
    coordinador: "luis",
    programas: ["informatica", "computacion"],
  },
  bd2Sql: {
    descripcion: "Consultas SQL con agregación",
    asignatura: "bd2",
    tipo: "practica",
    nivel: "medio",
    coordinador: "luis",
    programas: ["informatica"],
  },
  bd2Tx: {
    descripcion: "Transacciones y control de concurrencia",
    asignatura: "bd2",
    tipo: "laboratorio",
    nivel: "alto",
    coordinador: "luis",
    programas: ["informatica"],
  },
  estDescriptiva: {
    descripcion: "Estadística descriptiva",
    asignatura: "estadistica",
    tipo: "teoria",
    nivel: "bajo",
    coordinador: "luis",
    programas: ["informatica", "computacion"],
  },
  optLineal: {
    descripcion: "Programación lineal y método símplex",
    asignatura: "optimizacion",
    tipo: "practica",
    nivel: "alto",
    coordinador: "luis",
    programas: ["informatica"],
  },
  declHaskell: {
    descripcion: "Funciones de orden superior en Haskell",
    asignatura: "declarativa",
    tipo: "practica",
    nivel: "medio",
    coordinador: "elena",
    programas: ["computacion"],
  },
  declProlog: {
    descripcion: "Unificación y backtracking en Prolog",
    asignatura: "declarativa",
    tipo: "laboratorio",
    nivel: "alto",
    coordinador: "elena",
    programas: ["computacion"],
  },
};

const HORARIOS = [
  {
    programa: "informatica",
    coordinador: "luis",
    bloques: [
      ["bd2Intro", "2026-12-01"],
      ["bd2Normal", "2026-12-03"],
      ["bd2Sql", "2026-12-08"],
      ["bd2Tx", "2026-12-10"],
      ["estDescriptiva", "2026-12-01"],
      ["optLineal", "2026-12-15"],
    ],
  },
  {
    programa: "computacion",
    coordinador: "elena",
    bloques: [
      ["bd2Intro", "2026-12-02"],
      ["declHaskell", "2026-12-04"],
      ["declProlog", "2026-12-09"],
      ["estDescriptiva", "2026-12-11"],
    ],
  },
];

// ---------------------------------------------------------------------------
// Cliente HTTP
// ---------------------------------------------------------------------------

async function llamar(metodo, ruta, cuerpo) {
  let respuesta;
  try {
    respuesta = await fetch(API + ruta, {
      method: metodo,
      headers: cuerpo === undefined ? {} : { "Content-Type": "application/json" },
      body: cuerpo === undefined ? undefined : JSON.stringify(cuerpo),
    });
  } catch (error) {
    throw new Error(
      `No se pudo conectar con ${API}. ¿Está la API en marcha? (${error.cause?.code ?? error.message})`,
    );
  }
  const texto = await respuesta.text();
  const datos = texto ? JSON.parse(texto) : null;
  return { status: respuesta.status, datos };
}

async function exigir(metodo, ruta, cuerpo, esperados) {
  const { status, datos } = await llamar(metodo, ruta, cuerpo);
  if (!esperados.includes(status)) {
    const detalle = datos?.detail ?? datos?.title ?? JSON.stringify(datos);
    throw new Error(
      `${metodo} ${ruta} respondió ${status} (se esperaba ${esperados.join(" o ")}): ${detalle}`,
    );
  }
  return { status, datos };
}

function q(parametros) {
  return new URLSearchParams(
    Object.entries(parametros).filter(([, v]) => v !== undefined),
  ).toString();
}

const resumen = { creados: 0, existentes: 0 };

function informar(creado, texto) {
  if (creado) resumen.creados++;
  else resumen.existentes++;
  console.log(`  ${creado ? "+ creado   " : "= existente"} ${texto}`);
}

// ---------------------------------------------------------------------------
// Carga idempotente
// ---------------------------------------------------------------------------

async function asegurarPersona(nombre, perfil) {
  const { datos } = await exigir(
    "GET",
    `/api/personas?${q({ nombre, tipoPerfil: perfil.tipo, pageSize: 100 })}`,
    undefined,
    [200],
  );
  const existente = datos.items.find((p) => p.nombre === nombre);
  if (existente) {
    informar(false, `${perfil.tipo}: ${nombre}`);
    return existente.id;
  }
  const { datos: creada } = await exigir("POST", "/api/personas", { nombre, perfil }, [201]);
  informar(true, `${perfil.tipo}: ${nombre}`);
  return creada.id;
}

async function asegurarPorNombre(recurso, etiqueta, cuerpo) {
  const { datos } = await exigir(
    "GET",
    `/api/${recurso}?${q({ nombre: cuerpo.nombre, pageSize: 100 })}`,
    undefined,
    [200],
  );
  const existente = datos.items.find((x) => x.nombre === cuerpo.nombre);
  if (existente) {
    informar(false, `${etiqueta}: ${cuerpo.nombre}`);
    return existente.id;
  }
  const { datos: creado } = await exigir("POST", `/api/${recurso}`, cuerpo, [201]);
  informar(true, `${etiqueta}: ${cuerpo.nombre}`);
  return creado.id;
}

async function asegurarAutorizacion(coordinadorId, programaId, a, etiqueta) {
  const ruta = `/api/coordinadores/${coordinadorId}/autorizaciones`;
  const { datos } = await exigir("GET", `${ruta}?${q({ programaId })}`, undefined, [200]);
  if (datos.some((x) => x.fechaInicio === a.fechaInicio)) {
    informar(false, `autorización: ${etiqueta}`);
    return;
  }
  const cuerpo = {
    programaId,
    fechaInicio: a.fechaInicio,
    fechaFin: a.fechaFin,
    puedeGenerar: a.puedeGenerar,
    puedeValidar: a.puedeValidar,
  };
  await exigir("POST", ruta, cuerpo, [201]);
  informar(true, `autorización: ${etiqueta}`);
}

async function asegurarBloque(b, ids) {
  const asignaturaId = ids.asignaturas[b.asignatura];
  const { datos } = await exigir(
    "GET",
    `/api/bloques?${q({ asignaturaId, tipoActividad: b.tipo, pageSize: 100 })}`,
    undefined,
    [200],
  );
  const existente = datos.items.find((x) => x.descripcion === b.descripcion);
  if (existente) {
    informar(false, `bloque: ${b.descripcion}`);
    return existente.id;
  }
  const cuerpo = {
    descripcion: b.descripcion,
    tipoActividad: b.tipo,
    nivelComplejidad: b.nivel,
    asignaturaId,
    coordinadorId: ids.coordinadores[b.coordinador],
    programasIds: b.programas.map((p) => ids.programas[p]),
  };
  const { datos: creado } = await exigir("POST", "/api/bloques", cuerpo, [201]);
  informar(true, `bloque: ${b.descripcion}`);
  return creado.id;
}

async function asegurarHorario(h, ids) {
  const programaId = ids.programas[h.programa];
  const coordinadorId = ids.coordinadores[h.coordinador];
  const etiqueta = `horario ${PERIODO}: ${PROGRAMAS[h.programa].nombre}`;
  const { datos } = await exigir(
    "GET",
    `/api/horarios?${q({ programaId, periodoAcademico: PERIODO, pageSize: 100 })}`,
    undefined,
    [200],
  );
  const existente = datos.items.find((x) => x.coordinadorId === coordinadorId);
  if (existente) {
    informar(false, etiqueta);
    return existente.id;
  }
  const cuerpo = {
    programaId,
    periodoAcademico: PERIODO,
    coordinadorId,
    bloques: h.bloques.map(([clave, fecha]) => ({ bloqueId: ids.bloques[clave], fecha })),
  };
  const { datos: creado } = await exigir("POST", "/api/horarios", cuerpo, [201]);
  informar(true, etiqueta);
  return creado.id;
}

async function cargar() {
  const ids = {
    coordinadores: {},
    programas: {},
    asignaturas: {},
    estudiantes: {},
    bloques: {},
    horarios: [],
  };

  console.log("Personas");
  ids.jefe = await asegurarPersona(JEFE.nombre, JEFE.perfil);
  for (const [clave, c] of Object.entries(COORDINADORES)) {
    ids.coordinadores[clave] = await asegurarPersona(c.nombre, {
      tipo: "coordinador",
      especialidad: c.especialidad,
    });
  }
  for (const e of ESTUDIANTES) {
    const perfil = {
      tipo: "estudiante",
      edad: e.edad,
      grupo: e.grupo,
      coordinadorId: ids.coordinadores[e.coordinador],
    };
    ids.estudiantes[e.nombre] = await asegurarPersona(e.nombre, perfil);
  }

  console.log("Programas y asignaturas");
  for (const [clave, p] of Object.entries(PROGRAMAS)) {
    ids.programas[clave] = await asegurarPorNombre("programas", "programa", p);
  }
  for (const [clave, nombre] of Object.entries(ASIGNATURAS)) {
    ids.asignaturas[clave] = await asegurarPorNombre("asignaturas", "asignatura", { nombre });
  }
  for (const [programa, asignaturas] of Object.entries(PLANES)) {
    for (const a of asignaturas) {
      // La asociación es idempotente: responde 204 aunque ya exista.
      await exigir(
        "PUT",
        `/api/programas/${ids.programas[programa]}/asignaturas/${ids.asignaturas[a]}`,
        undefined,
        [204],
      );
    }
    console.log(
      `  = asociado   ${PROGRAMAS[programa].nombre}: ${asignaturas.map((a) => ASIGNATURAS[a]).join(", ")}`,
    );
  }

  console.log("Autorizaciones");
  for (const a of AUTORIZACIONES) {
    const etiqueta = `${COORDINADORES[a.coordinador].nombre} en ${PROGRAMAS[a.programa].nombre}`;
    await asegurarAutorizacion(
      ids.coordinadores[a.coordinador],
      ids.programas[a.programa],
      a,
      etiqueta,
    );
  }

  console.log("Matrículas");
  for (const e of ESTUDIANTES) {
    for (const p of e.programas) {
      const { status } = await exigir(
        "POST",
        `/api/programas/${ids.programas[p]}/matriculas`,
        { estudianteId: ids.estudiantes[e.nombre] },
        [201, 409],
      );
      informar(status === 201, `matrícula: ${e.nombre} en ${PROGRAMAS[p].nombre}`);
    }
  }

  console.log("Bloques de contenido");
  for (const [clave, b] of Object.entries(BLOQUES)) {
    ids.bloques[clave] = await asegurarBloque(b, ids);
  }

  console.log("Horarios");
  for (const h of HORARIOS) {
    ids.horarios.push(await asegurarHorario(h, ids));
  }

  return ids;
}

// ---------------------------------------------------------------------------
// Rechazos esperados
// ---------------------------------------------------------------------------

async function comprobarRechazos(ids) {
  const casos = [
    {
      texto: "Estudiante con edad 0",
      esperado: 400,
      llamada: () =>
        llamar("POST", "/api/personas", {
          nombre: "Estudiante inválido",
          perfil: {
            tipo: "estudiante",
            edad: 0,
            grupo: "C311",
            coordinadorId: ids.coordinadores.luis,
          },
        }),
    },
    {
      texto: "Estudiante cuyo coordinador es un jefe de departamento",
      esperado: 404,
      llamada: () =>
        llamar("POST", "/api/personas", {
          nombre: "Estudiante sin coordinador válido",
          perfil: { tipo: "estudiante", edad: 20, grupo: "C311", coordinadorId: ids.jefe },
        }),
    },
    {
      texto: "Matrícula repetida",
      esperado: 409,
      llamada: () =>
        llamar("POST", `/api/programas/${ids.programas.informatica}/matriculas`, {
          estudianteId: ids.estudiantes[ESTUDIANTES[0].nombre],
        }),
    },
    {
      texto: "Bloque duplicado con otros espacios y mayúsculas",
      esperado: 409,
      llamada: () =>
        llamar("POST", "/api/bloques", {
          descripcion: "  MODELO relacional   y álgebra RELACIONAL ",
          tipoActividad: "teoria",
          nivelComplejidad: "bajo",
          asignaturaId: ids.asignaturas.bd2,
          coordinadorId: ids.coordinadores.luis,
          programasIds: [],
        }),
    },
    {
      texto: "Bloque de Optimización asociado a Computación, que no tiene esa asignatura (RII03)",
      esperado: 400,
      llamada: () =>
        llamar(
          "PUT",
          `/api/bloques/${ids.bloques.optLineal}/programas/${ids.programas.computacion}`,
        ),
    },
    {
      texto: "Horario de un coordinador sin permiso de generación en el programa (RII01)",
      esperado: 403,
      llamada: () =>
        llamar("POST", "/api/horarios", {
          programaId: ids.programas.computacion,
          periodoAcademico: PERIODO,
          coordinadorId: ids.coordinadores.luis,
          bloques: [{ bloqueId: ids.bloques.bd2Intro, fecha: "2026-12-02" }],
        }),
    },
    {
      texto: "Horario con un bloque que no está asociado al programa",
      esperado: 409,
      llamada: () =>
        llamar("POST", "/api/horarios", {
          programaId: ids.programas.computacion,
          periodoAcademico: PERIODO,
          coordinadorId: ids.coordinadores.elena,
          bloques: [{ bloqueId: ids.bloques.bd2Sql, fecha: "2026-12-02" }],
        }),
    },
  ];

  console.log("Rechazos esperados");
  let fallos = 0;
  for (const caso of casos) {
    const { status, datos } = await caso.llamada();
    const ok = status === caso.esperado;
    if (!ok) fallos++;
    const detalle = datos?.detail ?? datos?.title ?? "";
    console.log(
      `  ${ok ? "ok   " : "FALLO"} ${status} (esperado ${caso.esperado}) ${caso.texto}${detalle ? ` — ${detalle}` : ""}`,
    );
  }
  return fallos;
}

// ---------------------------------------------------------------------------

async function main() {
  console.log(`API: ${API}\n`);
  const ids = await cargar();
  console.log(`\n${resumen.creados} registros creados y ${resumen.existentes} ya existentes.`);

  let fallos = 0;
  if (COMPROBAR_RECHAZOS) {
    console.log("");
    fallos = await comprobarRechazos(ids);
  }

  console.log("\nHorarios de la demostración:");
  for (const id of ids.horarios) console.log(`  ${API}/api/horarios/${id}`);

  if (fallos > 0) {
    console.error(`\n${fallos} rechazos no dieron el código esperado.`);
    process.exitCode = 1;
  }
}

main().catch((error) => {
  console.error(`\nError: ${error.message}`);
  process.exitCode = 1;
});
