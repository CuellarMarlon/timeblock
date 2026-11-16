import { useCallback, useMemo, useState } from "react";
import { listarPersonas, type Persona } from "../../api/persons";
import { useApiQuery } from "../../api/useApiQuery";
import { Modal } from "../components/Modal";
import { QueryContent } from "../components/QueryContent";
import { PersonaForm } from "./PersonaForm";
import { detallePerfil, TIPO_LABEL } from "./personaLabels";

const MAX_PAGE_SIZE = 100;
const PAGE_SIZE = 10;

/** Módulo de personas: cabecera con alta, listado con perfiles y formulario en modal. */
export function PersonasPanel() {
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  const load = useCallback(
    (signal: AbortSignal) => listarPersonas({ page: 1, pageSize: MAX_PAGE_SIZE }, signal),
    [],
  );
  const personas = useApiQuery(load);

  const nombres = useMemo(() => {
    if (personas.status !== "success") return new Map<string, string>();
    return new Map(personas.data.items.map((p) => [p.id, p.nombre]));
  }, [personas]);

  const coordinadores: Persona[] = useMemo(() => {
    if (personas.status !== "success") return [];
    return personas.data.items.filter((p) => p.perfil.tipo === "coordinador");
  }, [personas]);

  function handleSaved() {
    setCreating(false);
    setNotice("Persona creada correctamente.");
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
            setCreating(true);
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

      {creating && (
        <Modal title="Nueva persona" onClose={() => setCreating(false)}>
          <PersonaForm
            coordinadores={coordinadores}
            onSaved={handleSaved}
            onCancel={() => setCreating(false)}
          />
        </Modal>
      )}
    </section>
  );
}
