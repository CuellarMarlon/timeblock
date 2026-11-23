import { useCallback, useState } from "react";
import { listarAsignaturas, type Asignatura } from "../../api/program";
import { useApiQuery } from "../../api/useApiQuery";
import { Modal } from "../components/Modal";
import { QueryContent } from "../components/QueryContent";
import { AsignaturaForm } from "./AsignaturaForm";
import { AsociarAsignatura } from "./AsociarAsignatura";

/** Cantidad de asignaturas por página. */
const PAGE_SIZE = 10;

/** Pestaña de asignaturas: creación, edición, tabla paginada y asociación a programas. */
export function AsignaturasPanel() {
  const [page, setPage] = useState(1);
  const [refreshKey, setRefreshKey] = useState(0);
  const [editor, setEditor] = useState<{ asignatura?: Asignatura } | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const load = useCallback(
    (signal: AbortSignal) => listarAsignaturas({ page, pageSize: PAGE_SIZE }, signal),
    [page],
  );
  const asignaturas = useApiQuery(load);

  function handleSaved() {
    const editando = editor?.asignatura !== undefined;
    setEditor(null);
    setNotice(
      editando ? "Asignatura actualizada correctamente." : "Asignatura creada correctamente.",
    );
    asignaturas.reload();
    setRefreshKey((k) => k + 1);
  }

  return (
    <div className="module-panel">
      <div className="module-toolbar">
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => {
            setNotice(null);
            setEditor({});
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
                    <th scope="col">
                      <span className="visually-hidden">Acciones</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((asignatura) => (
                    <tr key={asignatura.id}>
                      <td>{asignatura.nombre}</td>
                      <td className="cell-actions">
                        <button
                          type="button"
                          className="btn-icon"
                          aria-label={`Editar ${asignatura.nombre}`}
                          onClick={() => {
                            setNotice(null);
                            setEditor({ asignatura });
                          }}
                        >
                          ✎
                        </button>
                      </td>
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

      <AsociarAsignatura refreshKey={refreshKey} />

      {editor && (
        <Modal
          title={editor.asignatura ? "Editar asignatura" : "Nueva asignatura"}
          onClose={() => setEditor(null)}
        >
          <AsignaturaForm
            key={editor.asignatura?.id ?? "nueva"}
            asignatura={editor.asignatura}
            onSaved={handleSaved}
            onCancel={() => setEditor(null)}
          />
        </Modal>
      )}
    </div>
  );
}
