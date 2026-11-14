import { useCallback, useId, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { CATALOGOS_VACIOS, cargarCatalogos, nombreDe } from "../../api/catalogos";
import { etiquetaEstadoHorario, listarHorarios, type FiltrosHorarios } from "../../api/horarios";
import { useApiQuery } from "../../api/useApiQuery";
import { ErrorMessage } from "../components/ErrorMessage";
import { Pagination } from "../components/Pagination";
import { QueryContent } from "../components/QueryContent";
import { SelectField } from "../components/SelectField";
import "../components/ui.css";
import { formatearFechaHora } from "../format";

const PAGE_SIZE = 20;

/** Listado de horarios con filtros por programa y periodo académico. */
export function HorariosPage() {
  const headingId = useId();
  const periodoId = useId();
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
    <section aria-labelledby={headingId}>
      <div className="page-header">
        <h1 id={headingId}>Horarios</h1>
        <Link className="button-link" to="/horarios/nuevo">
          Nuevo horario
        </Link>
      </div>
      <p>Consulta y creación manual de horarios docentes.</p>

      {catalogosQuery.status === "error" && (
        <ErrorMessage error={catalogosQuery.error} onRetry={catalogosQuery.reload} />
      )}

      <form className="filters" aria-label="Filtros de horarios" onSubmit={filtrarPorPeriodo}>
        <SelectField
          label="Programa"
          placeholder="Todos"
          value={filtros.programaId ?? ""}
          options={catalogos.programas.map((p) => ({ value: p.id, label: p.nombre }))}
          onChange={filtrarPorPrograma}
        />
        <div className="field">
          <label htmlFor={periodoId}>Periodo académico</label>
          <input
            id={periodoId}
            type="text"
            placeholder="2026-2027"
            value={periodo}
            onChange={(event) => setPeriodo(event.target.value)}
          />
        </div>
        <button type="submit">Filtrar</button>
      </form>

      <QueryContent
        query={horarios}
        onRetry={horarios.reload}
        loadingLabel="Cargando horarios…"
        emptyMessage="No hay horarios que coincidan con los filtros."
      >
        {(pagina) => (
          <>
            <div className="table-wrapper">
              <table className="data-table">
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
                        <span className="badge">{etiquetaEstadoHorario(horario.estado)}</span>
                      </td>
                      <td>{horario.cantTotalBloques}</td>
                      <td>
                        <Link
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
            </div>
            <Pagination
              page={pagina.page}
              pageSize={pagina.pageSize}
              totalItems={pagina.totalItems}
              onPageChange={setPage}
            />
          </>
        )}
      </QueryContent>
    </section>
  );
}
