import { useCallback, useRef, useState } from "react";
import { listarMatriculas, type Matricula } from "../../api/enrollments";
import { ApiError } from "../../api/errors";
import { obtenerPersona } from "../../api/persons";
import { obtenerPrograma } from "../../api/program";
import type { PagedResult } from "../../api/types";
import { useApiQuery } from "../../api/useApiQuery";
import { BuscadorSelect } from "../components/BuscadorSelect";
import { Modal } from "../components/Modal";
import { QueryContent } from "../components/QueryContent";
import { MatriculaForm } from "./MatriculaForm";
import { buscarEstudiantes, buscarProgramas } from "./matriculaBusquedas";
import "./ProgramasPage.css";

/** Filas por página en la tabla. */
const PAGE_SIZE = 10;

/** Matrícula con los nombres ya resueltos para mostrarla en la tabla. */
interface FilaMatricula extends Matricula {
  estudiante: string;
  programa: string;
}

/**
 * Consulta por id los nombres que aún no están en la caché. Un 404 deja el
 * nombre sin resolver; cualquier otro error hace fallar la carga de la tabla.
 */
async function resolverNombres(
  ids: string[],
  cache: Map<string, string>,
  obtener: (id: string, signal: AbortSignal) => Promise<{ nombre: string }>,
  signal: AbortSignal,
) {
  const faltan = [...new Set(ids)].filter((id) => !cache.has(id));
  await Promise.all(
    faltan.map(async (id) => {
      try {
        const registro = await obtener(id, signal);
        cache.set(id, registro.nombre);
      } catch (error) {
        if (!(error instanceof ApiError && error.status === 404)) throw error;
      }
    }),
  );
}

/** Módulo de matrícula: tabla filtrable por estudiante y programa, y alta en modal. */
export function MatriculaPage() {
  const [estudianteId, setEstudianteId] = useState("");
  const [programaId, setProgramaId] = useState("");
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);
  // Nombres ya consultados, para no repetir las peticiones al cambiar de página.
  const nombresPersonas = useRef(new Map<string, string>());
  const nombresProgramas = useRef(new Map<string, string>());

  const loadMatriculas = useCallback(
    async (signal: AbortSignal): Promise<PagedResult<FilaMatricula>> => {
      void refreshKey;
      const pagina = await listarMatriculas(
        { estudianteId: estudianteId || undefined, programaId: programaId || undefined },
        { page, pageSize: PAGE_SIZE },
        signal,
      );
      const personas = nombresPersonas.current;
      const programas = nombresProgramas.current;
      await Promise.all([
        resolverNombres(
          pagina.items.map((m) => m.estudianteId),
          personas,
          obtenerPersona,
          signal,
        ),
        resolverNombres(
          pagina.items.map((m) => m.programaId),
          programas,
          obtenerPrograma,
          signal,
        ),
      ]);
      return {
        ...pagina,
        items: pagina.items.map((m) => ({
          ...m,
          estudiante: personas.get(m.estudianteId) ?? "Estudiante desconocido",
          programa: programas.get(m.programaId) ?? "Programa desconocido",
        })),
      };
    },
    [estudianteId, programaId, page, refreshKey],
  );
  const matriculas = useApiQuery(loadMatriculas);

  function handleSaved() {
    setCreating(false);
    setNotice("Estudiante matriculado correctamente.");
    setRefreshKey((k) => k + 1);
  }

  return (
    <section className="module" aria-labelledby="matricula-titulo">
      <header className="page-header">
        <div>
          <h1 id="matricula-titulo">Matrícula</h1>
          <p>Relaciona estudiantes con los programas académicos en los que están matriculados.</p>
        </div>
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => {
            setNotice(null);
            setCreating(true);
          }}
        >
          + Nueva matrícula
        </button>
      </header>

      <div className="filters">
        <BuscadorSelect
          variant="filter"
          label="Filtrar por estudiante"
          searchLabel="Buscar estudiante"
          placeholder="Todos los estudiantes"
          value={estudianteId}
          buscar={buscarEstudiantes}
          onChange={(id) => {
            setEstudianteId(id);
            setPage(1);
          }}
        />
        <BuscadorSelect
          variant="filter"
          label="Filtrar por programa"
          searchLabel="Buscar programa"
          placeholder="Todos los programas"
          value={programaId}
          buscar={buscarProgramas}
          onChange={(id) => {
            setProgramaId(id);
            setPage(1);
          }}
        />
      </div>

      {notice && (
        <p className="notice" role="status">
          {notice}
        </p>
      )}

      <QueryContent
        query={matriculas}
        onRetry={matriculas.reload}
        loadingLabel="Cargando matrículas…"
        emptyMessage="No hay matrículas para los filtros seleccionados."
      >
        {(data) => {
          const totalPages = Math.max(1, Math.ceil(data.totalItems / data.pageSize));
          return (
            <div className="card">
              <table className="table">
                <thead>
                  <tr>
                    <th scope="col">Estudiante</th>
                    <th scope="col">Programa académico</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((m) => (
                    <tr key={`${m.estudianteId}-${m.programaId}`}>
                      <td>{m.estudiante}</td>
                      <td>{m.programa}</td>
                    </tr>
                  ))}
                </tbody>
              </table>

              <nav className="card-footer" aria-label="Paginación de matrículas">
                <button
                  type="button"
                  className="btn"
                  onClick={() => setPage(page - 1)}
                  disabled={page <= 1}
                >
                  Anterior
                </button>
                <span>
                  Página {data.page} de {totalPages}
                </span>
                <button
                  type="button"
                  className="btn"
                  onClick={() => setPage(page + 1)}
                  disabled={page >= totalPages}
                >
                  Siguiente
                </button>
              </nav>
            </div>
          );
        }}
      </QueryContent>

      {creating && (
        <Modal title="Nueva matrícula" onClose={() => setCreating(false)}>
          <MatriculaForm onSaved={handleSaved} onCancel={() => setCreating(false)} />
        </Modal>
      )}
    </section>
  );
}
