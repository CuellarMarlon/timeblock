import { useCallback, useId, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { CATALOGOS_VACIOS, cargarCatalogos, nombreDe } from "../../api/catalogos";
import { etiquetaEstadoHorario, listarHorarios, type FiltrosHorarios } from "../../api/horarios";
import { useApiQuery } from "../../api/useApiQuery";
import { ErrorMessage } from "../components/ErrorMessage";
import { FilterSelect } from "../components/FilterSelect";
import { Pagination } from "../components/Pagination";
import { QueryContent } from "../components/QueryContent";
import { formatearFechaHora } from "../format";
import "./BloquesHorarios.css";
import "./ProgramasPage.css";

const PAGE_SIZE = 20;

// Listado de horarios. Se puede filtrar por programa y por periodo académico
export function HorariosPage() {
  const headingId = useId();
  const [filtros, setFiltros] = useState<FiltrosHorarios>({});
  const [periodo, setPeriodo] = useState("");
  const [page, setPage] = useState(1);

  const catalogosQuery = useApiQuery(cargarCatalogos);
  const catalogos = catalogosQuery.status === "success" ? catalogosQuery.data : CATALOGOS_VACIOS;

  const load = useCallback(
    (signal: AbortSignal) => listarHorarios(filtros, { page, pageSize: PAGE_SIZE }, signal),
    [filtros, page],
  );
  const horarios = useApiQuery(load);

  function filtrarPorPrograma(programaId: string) {
    setFiltros((actuales) => ({ ...actuales, programaId: programaId || undefined }));
    setPage(1);
  }

  function filtrarPorPeriodo(event: FormEvent) {
    event.preventDefault();
    setFiltros((actuales) => ({ ...actuales, periodoAcademico: periodo.trim() || undefined }));
    setPage(1);
  }

  return (
    <section className="module" aria-labelledby={headingId}>
      <header className="page-header">
        <div>
          <h1 id={headingId}>Horarios</h1>
          <p>Consulta y creación manual de horarios docentes.</p>
        </div>
        <Link className="btn btn-primary" to="/horarios/nuevo">
          + Nuevo horario
        </Link>
      </header>

      {catalogosQuery.status === "error" && (
        <ErrorMessage error={catalogosQuery.error} onRetry={catalogosQuery.reload} />
      )}

      <form className="filters" aria-label="Filtros de horarios" onSubmit={filtrarPorPeriodo}>
        <FilterSelect
          label="Filtrar por programa"
          placeholder="Todos los programas"
          value={filtros.programaId ?? ""}
          options={catalogos.programas.map((p) => ({ value: p.id, label: p.nombre }))}
          onChange={filtrarPorPrograma}
        />
        <input
          type="text"
          aria-label="Filtrar por periodo académico"
          placeholder="Periodo académico (2026-2027)"
          value={periodo}
          onChange={(event) => setPeriodo(event.target.value)}
        />
        <button type="submit" className="btn">
          Filtrar
        </button>
      </form>

      <QueryContent
        query={horarios}
        onRetry={horarios.reload}
        loadingLabel="Cargando horarios…"
        emptyMessage="No hay horarios que coincidan con los filtros."
      >
        {(pagina) => (
          <div className="card">
            <table className="table">
              <thead>
                <tr>
                  <th scope="col">Programa</th>
                  <th scope="col">Periodo</th>
                  <th scope="col">Coordinador</th>
                  <th scope="col">Creado</th>
                  <th scope="col">Estado</th>
                  <th scope="col">Bloques</th>
                  <th scope="col">
                    <span className="visually-hidden">Acciones</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {pagina.items.map((horario) => (
                  <tr key={horario.id}>
                    <td>{nombreDe(catalogos.programas, horario.programaId)}</td>
                    <td>{horario.periodoAcademico}</td>
                    <td>{nombreDe(catalogos.coordinadores, horario.coordinadorId)}</td>
                    <td>{formatearFechaHora(horario.fechaCreacion)}</td>
                    <td>
                      <span className="badge badge--generado">
                        {etiquetaEstadoHorario(horario.estado)}
                      </span>
                    </td>
                    <td>{horario.cantTotalBloques}</td>
                    <td className="cell-actions">
                      <Link
                        className="btn"
                        to={`/horarios/${horario.id}`}
                        aria-label={`Ver horario ${horario.periodoAcademico} de ${nombreDe(catalogos.programas, horario.programaId)}`}
                      >
                        Ver
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            <Pagination
              label="Paginación de horarios"
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
