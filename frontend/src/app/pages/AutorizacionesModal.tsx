import { useCallback, useState } from "react";
import { listarAutorizaciones, type Autorizacion, type Persona } from "../../api/persons";
import { listarProgramas } from "../../api/program";
import { useApiQuery } from "../../api/useApiQuery";
import { Modal } from "../components/Modal";
import { QueryContent } from "../components/QueryContent";
import { AutorizacionForm } from "./AutorizacionForm";

const MAX_PAGE_SIZE = 100;

interface AutorizacionesModalProps {
  coordinador: Persona;
  onClose: () => void;
}

/** Modal con las autorizaciones de un coordinador: consulta, alta y actualización. */
export function AutorizacionesModal({ coordinador, onClose }: AutorizacionesModalProps) {
  const [editor, setEditor] = useState<{ autorizacion?: Autorizacion } | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const loadAutorizaciones = useCallback(
    (signal: AbortSignal) => listarAutorizaciones(coordinador.id, signal),
    [coordinador.id],
  );
  const loadProgramas = useCallback(
    (signal: AbortSignal) => listarProgramas({ page: 1, pageSize: MAX_PAGE_SIZE }, signal),
    [],
  );
  const autorizaciones = useApiQuery(loadAutorizaciones);
  const programas = useApiQuery(loadProgramas);

  const nombrePrograma = (id: string) =>
    programas.status === "success"
      ? (programas.data.items.find((p) => p.id === id)?.nombre ?? "Programa desconocido")
      : "…";

  function handleSaved() {
    const editando = editor?.autorizacion !== undefined;
    setEditor(null);
    setNotice(
      editando ? "Autorización actualizada correctamente." : "Autorización creada correctamente.",
    );
    autorizaciones.reload();
  }

  return (
    <Modal title={`Autorizaciones de ${coordinador.nombre}`} onClose={onClose}>
      {editor ? (
        programas.status === "success" ? (
          <AutorizacionForm
            key={
              editor.autorizacion
                ? `${editor.autorizacion.programaId}-${editor.autorizacion.fechaInicio}`
                : "nueva"
            }
            coordinadorId={coordinador.id}
            programas={programas.data.items}
            autorizacion={editor.autorizacion}
            onSaved={handleSaved}
            onCancel={() => setEditor(null)}
          />
        ) : (
          <p className="notice">Cargando programas…</p>
        )
      ) : (
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
              + Nueva autorización
            </button>
          </div>

          {notice && (
            <p className="notice" role="status">
              {notice}
            </p>
          )}

          <QueryContent
            query={autorizaciones}
            onRetry={autorizaciones.reload}
            loadingLabel="Cargando autorizaciones…"
            emptyMessage="Este coordinador todavía no tiene autorizaciones."
          >
            {(data) => (
              <div className="card">
                <table className="table">
                  <thead>
                    <tr>
                      <th scope="col">Programa</th>
                      <th scope="col">Inicio</th>
                      <th scope="col">Fin</th>
                      <th scope="col">Generar</th>
                      <th scope="col">Validar</th>
                      <th scope="col">
                        <span className="visually-hidden">Acciones</span>
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.map((a) => (
                      <tr key={`${a.programaId}-${a.fechaInicio}`}>
                        <td>{nombrePrograma(a.programaId)}</td>
                        <td>{a.fechaInicio}</td>
                        <td>{a.fechaFin ?? "—"}</td>
                        <td>{a.puedeGenerar ? "Sí" : "No"}</td>
                        <td>{a.puedeValidar ? "Sí" : "No"}</td>
                        <td className="cell-actions">
                          <button
                            type="button"
                            className="btn-icon"
                            aria-label={`Editar autorización de ${nombrePrograma(a.programaId)}`}
                            onClick={() => {
                              setNotice(null);
                              setEditor({ autorizacion: a });
                            }}
                          >
                            ✎
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </QueryContent>
        </div>
      )}
    </Modal>
  );
}
