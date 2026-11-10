import { useCallback, useState } from "react";
import { listarProgramas } from "../../api/program";
import { useApiQuery } from "../../api/useApiQuery";
import { Modal } from "../components/Modal";
import { QueryContent } from "../components/QueryContent";
import { ProgramaForm } from "./ProgramaForm";

/** Cantidad de programas por página. */
const PAGE_SIZE = 10;

/** Pestaña de programas: botón de creación, tabla paginada y formulario en modal. */
export function ProgramasPanel() {
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  const load = useCallback(
    (signal: AbortSignal) => listarProgramas({ page, pageSize: PAGE_SIZE }, signal),
    [page],
  );
  const programas = useApiQuery(load);

  function handleCreated() {
    setCreating(false);
    setNotice("Programa creado correctamente.");
    programas.reload();
  }

  return (
    <div className="module-panel">
      <div className="module-toolbar">
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => {
            setNotice(null);
            setCreating(true);
          }}
        >
          + Nuevo programa
        </button>
      </div>

      {notice && (
        <p className="notice" role="status">
          {notice}
        </p>
      )}

      <QueryContent
        query={programas}
        onRetry={programas.reload}
        loadingLabel="Cargando programas…"
        emptyMessage="Todavía no hay programas registrados."
      >
        {(data) => {
          const totalPages = Math.max(1, Math.ceil(data.totalItems / data.pageSize));
          return (
            <div className="card">
              <table className="table">
                <thead>
                  <tr>
                    <th scope="col">Nombre</th>
                    <th scope="col">Plan de estudios</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((programa) => (
                    <tr key={programa.id}>
                      <td>{programa.nombre}</td>
                      <td>{programa.planEstudio}</td>
                    </tr>
                  ))}
                </tbody>
              </table>

              <nav className="card-footer" aria-label="Paginación de programas">
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
        <Modal title="Nuevo programa" onClose={() => setCreating(false)}>
          <ProgramaForm onCreated={handleCreated} onCancel={() => setCreating(false)} />
        </Modal>
      )}
    </div>
  );
}
