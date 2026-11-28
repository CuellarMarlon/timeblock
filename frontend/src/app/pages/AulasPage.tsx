import { useCallback, useId, useState, type FormEvent } from "react";
import { Link, useLocation } from "react-router-dom";
import { listarAulas, type FiltrosAulas } from "../../api/aulas";
import { useApiQuery } from "../../api/useApiQuery";
import { Pagination } from "../components/Pagination";
import { QueryContent } from "../components/QueryContent";
import { SuccessMessage } from "../components/SuccessMessage";
import "./BloquesHorarios.css";
import "./ProgramasPage.css";

const PAGE_SIZE = 20;

export function AulasPage() {
  const headingId = useId();
  const location = useLocation();
  const aviso = (location.state as { aviso?: string } | null)?.aviso;

  const [filtros, setFiltros] = useState<FiltrosAulas>({});
  const [edificio, setEdificio] = useState("");
  const [page, setPage] = useState(1);

  const load = useCallback(
    (signal: AbortSignal) => listarAulas(filtros, { page, pageSize: PAGE_SIZE }, signal),
    [filtros, page],
  );
  const aulas = useApiQuery(load);

  function filtrar(event: FormEvent) {
    event.preventDefault();
    setFiltros({ edificio: edificio.trim() || undefined });
    setPage(1);
  }

  return (
    <section className="module" aria-labelledby={headingId}>
      <header className="page-header">
        <div>
          <h1 id={headingId}>Aulas</h1>
          <p>Registro y consulta de las aulas disponibles, con su edificio y capacidad.</p>
        </div>
        <Link className="btn btn-primary" to="/aulas/nueva">
          + Nueva aula
        </Link>
      </header>

      {aviso && <SuccessMessage message={aviso} />}

      <form className="filters" aria-label="Filtros de aulas" onSubmit={filtrar}>
        <input
          type="text"
          aria-label="Filtrar por edificio"
          placeholder="Edificio"
          value={edificio}
          onChange={(event) => setEdificio(event.target.value)}
        />
        <button type="submit" className="btn">
          Filtrar
        </button>
      </form>

      <QueryContent
        query={aulas}
        onRetry={aulas.reload}
        loadingLabel="Cargando aulas…"
        emptyMessage={
          filtros.edificio
            ? "No hay aulas que coincidan con el filtro."
            : "Todavía no hay aulas registradas."
        }
      >
        {(pagina) => (
          <div className="card">
            <table className="table">
              <thead>
                <tr>
                  <th scope="col">Edificio</th>
                  <th scope="col">Capacidad</th>
                  <th scope="col">
                    <span className="visually-hidden">Acciones</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {pagina.items.map((aula) => (
                  <tr key={aula.id}>
                    <td>{aula.edificio}</td>
                    <td>{aula.capacidad}</td>
                    <td className="cell-actions">
                      <Link
                        className="btn-icon"
                        to={`/aulas/${aula.id}/editar`}
                        aria-label={`Editar aula de ${aula.edificio} con capacidad ${aula.capacidad}`}
                      >
                        ✎
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            <Pagination
              label="Paginación de aulas"
              page={pagina.page}
              pageSize={pagina.pageSize}
              totalItems={pagina.totalItems}
              onPageChange={setPage}
            />
          </div>
        )}
      </QueryContent>
    </section>
  );
}
