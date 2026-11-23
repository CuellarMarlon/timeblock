import { useCallback, useMemo, useState } from "react";
import { listarMatriculas } from "../../api/enrollments";
import { listarPersonas } from "../../api/persons";
import { listarProgramas } from "../../api/program";
import { useApiQuery } from "../../api/useApiQuery";
import { Modal } from "../components/Modal";
import { QueryContent } from "../components/QueryContent";
import { MatriculaForm } from "./MatriculaForm";
import "./ProgramasPage.css";

/** Máximo de elementos por página admitido por la API. */
const MAX_PAGE_SIZE = 100;
/** Filas por página en la tabla. */
const PAGE_SIZE = 10;

/** Módulo de matrícula: tabla filtrable por estudiante y programa, y alta en modal. */
export function MatriculaPage() {
  const [estudianteId, setEstudianteId] = useState("");
  const [programaId, setProgramaId] = useState("");
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);

  const loadProgramas = useCallback(
    (signal: AbortSignal) => listarProgramas({ page: 1, pageSize: MAX_PAGE_SIZE }, signal),
    [],
  );
  const loadPersonas = useCallback(
    (signal: AbortSignal) => listarPersonas({ page: 1, pageSize: MAX_PAGE_SIZE }, signal),
    [],
  );
  const programas = useApiQuery(loadProgramas);
  const personas = useApiQuery(loadPersonas);

  const listaProgramas = useMemo(
    () => (programas.status === "success" ? programas.data.items : []),
    [programas],
  );
  const estudiantes = useMemo(
    () =>
      personas.status === "success"
        ? personas.data.items.filter((p) => p.perfil.tipo === "estudiante")
        : [],
    [personas],
  );
  const nombresProgramas = useMemo(
    () => new Map(listaProgramas.map((p) => [p.id, p.nombre])),
    [listaProgramas],
  );
  const nombresPersonas = useMemo(
    () =>
      new Map(
        personas.status === "success" ? personas.data.items.map((p) => [p.id, p.nombre]) : [],
      ),
    [personas],
  );

  const loadMatriculas = useCallback(
    (signal: AbortSignal) => {
      void refreshKey;
      return listarMatriculas(
        { estudianteId: estudianteId || undefined, programaId: programaId || undefined },
        { page, pageSize: PAGE_SIZE },
        signal,
      );
    },
    [estudianteId, programaId, page, refreshKey],
  );
  const matriculas = useApiQuery(loadMatriculas);

  const listasListas = programas.status === "success" && personas.status === "success";

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
          disabled={!listasListas}
          onClick={() => {
            setNotice(null);
            setCreating(true);
          }}
        >
          + Nueva matrícula
        </button>
      </header>

      <div className="filters">
        <select
          aria-label="Filtrar por estudiante"
          value={estudianteId}
          onChange={(e) => {
            setEstudianteId(e.target.value);
            setPage(1);
          }}
        >
          <option value="">Todos los estudiantes</option>
          {estudiantes.map((e) => (
            <option key={e.id} value={e.id}>
              {e.nombre}
            </option>
          ))}
        </select>
        <select
          aria-label="Filtrar por programa"
          value={programaId}
          onChange={(e) => {
            setProgramaId(e.target.value);
            setPage(1);
          }}
        >
          <option value="">Todos los programas</option>
          {listaProgramas.map((p) => (
            <option key={p.id} value={p.id}>
              {p.nombre}
            </option>
          ))}
        </select>
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
                      <td>{nombresPersonas.get(m.estudianteId) ?? "Estudiante desconocido"}</td>
                      <td>{nombresProgramas.get(m.programaId) ?? "Programa desconocido"}</td>
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
          <MatriculaForm
            estudiantes={estudiantes}
            programas={listaProgramas}
            onSaved={handleSaved}
            onCancel={() => setCreating(false)}
          />
        </Modal>
      )}
    </section>
  );
}
