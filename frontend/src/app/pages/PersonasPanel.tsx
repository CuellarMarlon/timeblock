import { useCallback, useMemo, useState } from "react";
import { listarPersonas } from "../../api/persons";
import { useApiQuery } from "../../api/useApiQuery";
import { QueryContent } from "../components/QueryContent";
import { detallePerfil, TIPO_LABEL } from "./personaLabels";

const MAX_PAGE_SIZE = 100;
const PAGE_SIZE = 10;

/** Listado de personas con su perfil */
export function PersonasPanel() {
  const [page, setPage] = useState(1);

  const load = useCallback(
    (signal: AbortSignal) => listarPersonas({ page: 1, pageSize: MAX_PAGE_SIZE }, signal),
    [],
  );
  const personas = useApiQuery(load);

  const nombres = useMemo(() => {
    if (personas.status !== "success") return new Map<string, string>();
    return new Map(personas.data.items.map((p) => [p.id, p.nombre]));
  }, [personas]);

  return (
    <div className="module-panel">
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
    </div>
  );
}
