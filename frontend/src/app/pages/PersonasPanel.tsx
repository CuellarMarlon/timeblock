import { useCallback, useMemo, useState } from "react";
import { eliminarPersona, listarPersonas, type Persona } from "../../api/persons";
import { useApiMutation } from "../../api/useApiMutation";
import { useApiQuery } from "../../api/useApiQuery";
import { ErrorMessage } from "../components/ErrorMessage";
import { Modal } from "../components/Modal";
import { QueryContent } from "../components/QueryContent";
import { PersonaForm } from "./PersonaForm";
import { detallePerfil, TIPO_LABEL } from "./personaLabels";
import { AutorizacionesModal } from "./AutorizacionesModal";

const MAX_PAGE_SIZE = 100;
const PAGE_SIZE = 10;

/** Módulo de personas: cabecera con alta, listado con perfiles y formulario en modal. */
export function PersonasPanel() {
  const [page, setPage] = useState(1);
  const [editor, setEditor] = useState<{ persona?: Persona } | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [autorizando, setAutorizando] = useState<Persona | null>(null);
  const [pendiente, setPendiente] = useState<Persona | null>(null);
  const eliminar = useApiMutation(eliminarPersona);
  const eliminando = eliminar.state.status === "loading";
  const load = useCallback(
    (signal: AbortSignal) => listarPersonas({ page: 1, pageSize: MAX_PAGE_SIZE }, signal),
    [],
  );
  const personas = useApiQuery(load);

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
      setNotice("Persona eliminada correctamente.");
      setPage(1);
      personas.reload();
    }
  }

  const nombres = useMemo(() => {
    if (personas.status !== "success") return new Map<string, string>();
    return new Map(personas.data.items.map((p) => [p.id, p.nombre]));
  }, [personas]);

  const coordinadores: Persona[] = useMemo(() => {
    if (personas.status !== "success") return [];
    return personas.data.items.filter((p) => p.perfil.tipo === "coordinador");
  }, [personas]);

  function handleSaved() {
    const editando = editor?.persona !== undefined;
    setEditor(null);
    setNotice(editando ? "Persona actualizada correctamente." : "Persona creada correctamente.");
    personas.reload();
  }

  return (
    <section className="module" aria-labelledby="personas-titulo">
      <header className="page-header">
        <div>
          <h1 id="personas-titulo">Personas y perfiles</h1>
          <p>Gestión de estudiantes, coordinadores académicos y jefes de departamento.</p>
        </div>
        <button
          type="button"
          className="btn btn-primary"
          disabled={personas.status !== "success"}
          onClick={() => {
            setNotice(null);
            setEditor({});
          }}
        >
          + Nueva persona
        </button>
      </header>

      {notice && (
        <p className="notice" role="status">
          {notice}
        </p>
      )}

      <QueryContent
        query={personas}
        onRetry={personas.reload}
        loadingLabel="Cargando personas…"
        emptyMessage="Todavía no hay personas registradas."
      >
        {(data) => {
          const totalPages = Math.max(1, Math.ceil(data.items.length / PAGE_SIZE));
          const visibles = data.items.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
          return (
            <div className="card">
              <table className="table">
                <thead>
                  <tr>
                    <th scope="col">Nombre</th>
                    <th scope="col">Perfil</th>
                    <th scope="col">Detalle</th>
                    <th scope="col">
                      <span className="visually-hidden">Acciones</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {visibles.map((persona) => (
                    <tr key={persona.id}>
                      <td>{persona.nombre}</td>
                      <td>
                        <span className={`badge badge--${persona.perfil.tipo}`}>
                          {TIPO_LABEL[persona.perfil.tipo]}
                        </span>
                      </td>
                      <td>
                        {detallePerfil(
                          persona.perfil,
                          persona.perfil.tipo === "estudiante"
                            ? nombres.get(persona.perfil.coordinadorId)
                            : undefined,
                        )}
                      </td>
                      <td className="cell-actions">
                        {persona.perfil.tipo === "coordinador" && (
                          <button
                            type="button"
                            className="btn"
                            onClick={() => setAutorizando(persona)}
                          >
                            Autorizaciones
                          </button>
                        )}
                        <button
                          type="button"
                          className="btn-icon"
                          aria-label={`Editar ${persona.nombre}`}
                          onClick={() => {
                            setNotice(null);
                            setEditor({ persona });
                          }}
                        >
                          ✎
                        </button>
                        <button
                          type="button"
                          className="btn"
                          aria-label={`Eliminar ${persona.nombre}`}
                          onClick={() => {
                            setNotice(null);
                            eliminar.reset();
                            setPendiente(persona);
                          }}
                        >
                          Eliminar
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>

              <nav className="card-footer" aria-label="Paginación de personas">
                <button
                  type="button"
                  className="btn"
                  onClick={() => setPage(page - 1)}
                  disabled={page <= 1}
                >
                  Anterior
                </button>
                <span>
                  Página {page} de {totalPages}
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
          title={editor.persona ? "Editar persona" : "Nueva persona"}
          onClose={() => setEditor(null)}
        >
          <PersonaForm
            key={editor.persona?.id ?? "nueva"}
            persona={editor.persona}
            coordinadores={coordinadores}
            onSaved={handleSaved}
            onCancel={() => setEditor(null)}
          />
        </Modal>
      )}
      {autorizando && (
        <AutorizacionesModal coordinador={autorizando} onClose={() => setAutorizando(null)} />
      )}
      {pendiente && (
        <Modal title="Confirmar eliminación" onClose={cancelarEliminacion}>
          <p>
            ¿Deseas eliminar a <strong>{pendiente.nombre}</strong> y su perfil?
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
    </section>
  );
}
