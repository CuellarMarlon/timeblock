import { useCallback, useId, useState } from "react";
import { Link } from "react-router-dom";
import {
  etiquetaNivelComplejidad,
  etiquetaTipoActividad,
  listarBloques,
  NIVELES_COMPLEJIDAD,
  TIPOS_ACTIVIDAD,
  type FiltrosBloques,
} from "../../api/bloques";
import { CATALOGOS_VACIOS, cargarCatalogos, nombreDe } from "../../api/catalogos";
import { useApiQuery } from "../../api/useApiQuery";
import { ErrorMessage } from "../components/ErrorMessage";
import { FilterSelect } from "../components/FilterSelect";
import { Pagination } from "../components/Pagination";
import { QueryContent } from "../components/QueryContent";
import "./BloquesHorarios.css";
import "./ProgramasPage.css";

const PAGE_SIZE = 20;

// Listado de bloques de contenido. Se puede filtrar por programa, asignatura,
// coordinador, tipo de actividad y complejidad
export function BloquesPage() {
  const headingId = useId();
  const [filtros, setFiltros] = useState<FiltrosBloques>({});
  const [page, setPage] = useState(1);

  const catalogosQuery = useApiQuery(cargarCatalogos);
  const catalogos = catalogosQuery.status === "success" ? catalogosQuery.data : CATALOGOS_VACIOS;

  const load = useCallback(
    (signal: AbortSignal) => listarBloques(filtros, { page, pageSize: PAGE_SIZE }, signal),
    [filtros, page],
  );
  const bloques = useApiQuery(load);

  // Cambia un filtro y vuelve a la primera página. Un valor vacío quita el filtro
  function cambiarFiltro<K extends keyof FiltrosBloques>(clave: K, valor: string) {
    setFiltros((actuales) => ({ ...actuales, [clave]: (valor || undefined) as FiltrosBloques[K] }));
    setPage(1);
  }

  return (
    <section className="module" aria-labelledby={headingId}>
      <header className="page-header">
        <div>
          <h1 id={headingId}>Bloques de contenido</h1>
          <p>Gestión y clasificación de los bloques de contenido a impartir.</p>
        </div>
        <Link className="btn btn-primary" to="/bloques/nuevo">
          + Nuevo bloque
        </Link>
      </header>

      {catalogosQuery.status === "error" && (
        <ErrorMessage error={catalogosQuery.error} onRetry={catalogosQuery.reload} />
      )}

      <form
        className="filters"
        aria-label="Filtros de bloques"
        onSubmit={(e) => e.preventDefault()}
      >
        <FilterSelect
          label="Filtrar por programa"
          placeholder="Todos los programas"
          value={filtros.programaId ?? ""}
          options={catalogos.programas.map((p) => ({ value: p.id, label: p.nombre }))}
          onChange={(valor) => cambiarFiltro("programaId", valor)}
        />
        <FilterSelect
          label="Filtrar por asignatura"
          placeholder="Todas las asignaturas"
          value={filtros.asignaturaId ?? ""}
          options={catalogos.asignaturas.map((a) => ({ value: a.id, label: a.nombre }))}
          onChange={(valor) => cambiarFiltro("asignaturaId", valor)}
        />
        <FilterSelect
          label="Filtrar por coordinador"
          placeholder="Todos los coordinadores"
          value={filtros.coordinadorId ?? ""}
          options={catalogos.coordinadores.map((c) => ({ value: c.id, label: c.nombre }))}
          onChange={(valor) => cambiarFiltro("coordinadorId", valor)}
        />
        <FilterSelect
          label="Filtrar por tipo de actividad"
          placeholder="Todos los tipos de actividad"
          value={filtros.tipoActividad ?? ""}
          options={TIPOS_ACTIVIDAD}
          onChange={(valor) => cambiarFiltro("tipoActividad", valor)}
        />
        <FilterSelect
          label="Filtrar por complejidad"
          placeholder="Todas las complejidades"
          value={filtros.nivelComplejidad ?? ""}
          options={NIVELES_COMPLEJIDAD}
          onChange={(valor) => cambiarFiltro("nivelComplejidad", valor)}
        />
      </form>

      <QueryContent
        query={bloques}
        onRetry={bloques.reload}
        loadingLabel="Cargando bloques…"
        emptyMessage="No hay bloques que coincidan con los filtros."
      >
        {(pagina) => (
          <div className="card">
            <table className="table">
              <thead>
                <tr>
                  <th scope="col">Descripción</th>
                  <th scope="col">Asignatura</th>
                  <th scope="col">Actividad</th>
                  <th scope="col">Complejidad</th>
                  <th scope="col">Coordinador</th>
                  <th scope="col">Programas</th>
                  <th scope="col">
                    <span className="visually-hidden">Acciones</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {pagina.items.map((bloque) => (
                  <tr key={bloque.id}>
                    <td>
                      <Link to={`/bloques/${bloque.id}`}>{bloque.descripcion}</Link>
                    </td>
                    <td>{nombreDe(catalogos.asignaturas, bloque.asignaturaId)}</td>
                    <td>{etiquetaTipoActividad(bloque.tipoActividad)}</td>
                    <td>{etiquetaNivelComplejidad(bloque.nivelComplejidad)}</td>
                    <td>{nombreDe(catalogos.coordinadores, bloque.coordinadorId)}</td>
                    <td>
                      {bloque.programasIds.length > 0
                        ? bloque.programasIds
                            .map((id) => nombreDe(catalogos.programas, id))
                            .join(", ")
                        : "Sin programas"}
                    </td>
                    <td className="cell-actions">
                      <Link
                        className="btn-icon"
                        to={`/bloques/${bloque.id}/editar`}
                        aria-label={`Editar ${bloque.descripcion}`}
                      >
                        ✎
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            <Pagination
              label="Paginación de bloques"
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
