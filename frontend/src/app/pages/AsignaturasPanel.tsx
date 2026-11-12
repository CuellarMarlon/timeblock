import { useCallback, useState } from "react";
import { listarAsignaturas } from "../../api/program";
import { useApiQuery } from "../../api/useApiQuery";
import { Modal } from "../components/Modal";
import { QueryContent } from "../components/QueryContent";
import { AsignaturaForm } from "./AsignaturaForm";
import { AsociarAsignatura } from "./AsociarAsignatura";
const PAGE_SIZE = 10;

/** Pestaña de asignaturas: botón de creación, tabla paginada y formulario en modal. */
export function AsignaturasPanel() {
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  const load = useCallback(
    (signal: AbortSignal) => listarAsignaturas({ page, pageSize: PAGE_SIZE }, signal),
    [page],
  );
  const asignaturas = useApiQuery(load);

  function handleCreated() {
    setCreating(false);
    setNotice("Asignatura creada correctamente.");
    asignaturas.reload();
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
          + Nueva asignatura
        </button>
      </div>

      {notice && (
        <p className="notice" role="status">
          {notice}
        </p>
      )}

      <QueryContent
        query={asignaturas}
        onRetry={asignaturas.reload}
        loadingLabel="Cargando asignaturas…"
        emptyMessage="Todavía no hay asignaturas registradas."
      >
        {(data) => {
          const totalPages = Math.max(1, Math.ceil(data.totalItems / data.pageSize));
          return (
            <div className="card">
              <table className="table">
                <thead>
                  <tr>
                    <th scope="col">Nombre</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((asignatura) => (
                    <tr key={asignatura.id}>
                      <td>{asignatura.nombre}</td>
                    </tr>
                  ))}
                </tbody>
              </table>

              <nav className="card-footer" aria-label="Paginación de asignaturas">
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
      <AsociarAsignatura />
      {creating && (
        <Modal title="Nueva asignatura" onClose={() => setCreating(false)}>
          <AsignaturaForm onCreated={handleCreated} onCancel={() => setCreating(false)} />
        </Modal>
      )}
    </div>
  );
}
