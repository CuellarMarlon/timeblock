import { useCallback, useState } from "react";
import { listarProgramas } from "../../api/program";
import { useApiQuery } from "../../api/useApiQuery";
import { QueryContent } from "../components/QueryContent";
import "./ProgramasPage.css";

/** Cantidad de programas por página. */
const PAGE_SIZE = 10;

/** Módulo de programas académicos: listado paginado conectado a la API. */
export function ProgramasPage() {
  const [page, setPage] = useState(1);

  const load = useCallback(
    (signal: AbortSignal) => listarProgramas({ page, pageSize: PAGE_SIZE }, signal),
    [page],
  );
  const programas = useApiQuery(load);

  return (
    <section className="module" aria-labelledby="programas-titulo">
      <h1 id="programas-titulo">Programas y asignaturas</h1>
      <p>Gestión de los programas académicos, sus planes de estudio y asignaturas.</p>

      <h2>Programas</h2>
      <QueryContent
        query={programas}
        onRetry={programas.reload}
        loadingLabel="Cargando programas…"
        emptyMessage="Todavía no hay programas registrados."
      >
        {(data) => {
          const totalPages = Math.max(1, Math.ceil(data.totalItems / data.pageSize));
          return (
            <>
              <table className="module-table">
                <thead>
                  <tr>
                    <th scope="col">Nombre</th>
                    <th scope="col">Plan de estudio</th>
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

              <nav className="module-pager" aria-label="Paginación de programas">
                <button type="button" onClick={() => setPage(page - 1)} disabled={page <= 1}>
                  Anterior
                </button>
                <span>
                  Página {data.page} de {totalPages}
                </span>
                <button
                  type="button"
                  onClick={() => setPage(page + 1)}
                  disabled={page >= totalPages}
                >
                  Siguiente
                </button>
              </nav>
            </>
          );
        }}
      </QueryContent>
    </section>
  );
}
