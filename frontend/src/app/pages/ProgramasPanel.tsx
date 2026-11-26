import { useCallback, useState } from "react";
import { eliminarPrograma, listarProgramas, type Programa } from "../../api/program";
import { useApiMutation } from "../../api/useApiMutation";
import { useApiQuery } from "../../api/useApiQuery";
import { ErrorMessage } from "../components/ErrorMessage";
import { Modal } from "../components/Modal";
import { QueryContent } from "../components/QueryContent";
import { ProgramaForm } from "./ProgramaForm";

/** Cantidad de programas por página. */
const PAGE_SIZE = 10;

/** Pestaña de programas: botón de creación, tabla paginada y formulario en modal. */
export function ProgramasPanel() {
  const [page, setPage] = useState(1);
  const [editor, setEditor] = useState<{ programa?: Programa } | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [pendiente, setPendiente] = useState<Programa | null>(null);
  const eliminar = useApiMutation(eliminarPrograma);
  const eliminando = eliminar.state.status === "loading";

  const load = useCallback(
    (signal: AbortSignal) => listarProgramas({ page, pageSize: PAGE_SIZE }, signal),
    [page],
  );
  const programas = useApiQuery(load);

  function cancelarEliminacion() {
    if (eliminando) return;
    setPendiente(null);
    eliminar.reset();
  }

  async function confirmarEliminacion() {
    if (!pendiente) return;
    const resultado = await eliminar.run(pendiente.id);
    if (resultado.ok) {
      setPendiente(null);
      setNotice("Programa eliminado correctamente.");
      setPage(1);
      programas.reload();
    }
  }

  function handleSaved() {
    const editando = editor?.programa !== undefined;
    setEditor(null);
    setNotice(editando ? "Programa actualizado correctamente." : "Programa creado correctamente.");
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
            setEditor({});
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
                    <th scope="col">
                      <span className="visually-hidden">Acciones</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((programa) => (
                    <tr key={programa.id}>
                      <td>{programa.nombre}</td>
                      <td>{programa.planEstudio}</td>
                      <td className="cell-actions">
                        <button
                          type="button"
                          className="btn-icon"
                          aria-label={`Editar ${programa.nombre}`}
                          onClick={() => {
                            setNotice(null);
                            setEditor({ programa });
                          }}
                        >
                          ✎
                        </button>
                        <button
                          type="button"
                          className="btn"
                          aria-label={`Eliminar ${programa.nombre}`}
                          onClick={() => {
                            setNotice(null);
                            eliminar.reset();
                            setPendiente(programa);
                          }}
                        >
                          Eliminar
                        </button>
                      </td>
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

      {editor && (
        <Modal
          title={editor.programa ? "Editar programa" : "Nuevo programa"}
          onClose={() => setEditor(null)}
        >
          <ProgramaForm
            key={editor.programa?.id ?? "nuevo"}
            programa={editor.programa}
            onSaved={handleSaved}
            onCancel={() => setEditor(null)}
          />
        </Modal>
      )}
      {pendiente && (
        <Modal title="Confirmar eliminación" onClose={cancelarEliminacion}>
          <p>
            ¿Deseas eliminar el programa <strong>{pendiente.nombre}</strong>?
          </p>
          {eliminar.state.status === "error" && <ErrorMessage error={eliminar.state.error} />}
          <div className="form-actions">
            <button
              type="button"
              className="btn"
              onClick={cancelarEliminacion}
              disabled={eliminando}
            >
              Cancelar
            </button>
            <button
              type="button"
              className="btn btn-primary"
              onClick={confirmarEliminacion}
              disabled={eliminando}
            >
              {eliminando ? "Eliminando…" : "Confirmar eliminación"}
            </button>
          </div>
        </Modal>
      )}
    </div>
  );
}
