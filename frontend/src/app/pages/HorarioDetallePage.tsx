import { useCallback, useId } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import { etiquetaNivelComplejidad, etiquetaTipoActividad } from "../../api/bloques";
import { CATALOGOS_VACIOS, cargarCatalogos, nombreDe } from "../../api/catalogos";
import { etiquetaEstadoHorario, obtenerHorario } from "../../api/horarios";
import { useApiQuery } from "../../api/useApiQuery";
import { ErrorMessage } from "../components/ErrorMessage";
import { QueryContent } from "../components/QueryContent";
import { SuccessMessage } from "../components/SuccessMessage";
import "../components/ui.css";
import { formatearFechaHora } from "../format";

/** Consulta de un horario con sus bloques y la fecha de cada uno. */
export function HorarioDetallePage() {
  const headingId = useId();
  const { horarioId = "" } = useParams();
  const location = useLocation();
  // Aviso enviado por el formulario tras crear el horario.
  const aviso = (location.state as { aviso?: string } | null)?.aviso;

  const catalogosQuery = useApiQuery(cargarCatalogos);
  const catalogos = catalogosQuery.status === "success" ? catalogosQuery.data : CATALOGOS_VACIOS;

  const load = useCallback((signal: AbortSignal) => obtenerHorario(horarioId, signal), [horarioId]);
  const horario = useApiQuery(load);

  return (
    <section aria-labelledby={headingId}>
      <h1 id={headingId}>Detalle del horario</h1>
      <p>
        <Link to="/horarios">← Volver a los horarios</Link>
      </p>

      {aviso && <SuccessMessage message={aviso} />}
      {catalogosQuery.status === "error" && (
        <ErrorMessage error={catalogosQuery.error} onRetry={catalogosQuery.reload} />
      )}

      <QueryContent
        query={horario}
        onRetry={horario.reload}
        loadingLabel="Cargando horario…"
        isEmpty={() => false}
      >
        {(datos) => (
          <>
            <dl className="detail-list">
              <dt>Programa</dt>
              <dd>{nombreDe(catalogos.programas, datos.programaId)}</dd>
              <dt>Periodo académico</dt>
              <dd>{datos.periodoAcademico}</dd>
              <dt>Coordinador</dt>
              <dd>{nombreDe(catalogos.coordinadores, datos.coordinadorId)}</dd>
              <dt>Fecha de creación</dt>
              <dd>{formatearFechaHora(datos.fechaCreacion)}</dd>
              <dt>Estado</dt>
              <dd>
                <span className="badge">{etiquetaEstadoHorario(datos.estado)}</span>
              </dd>
              <dt>Bloques distintos</dt>
              <dd>{datos.cantTotalBloques}</dd>
            </dl>

            <h2>Bloques y fechas</h2>
            {datos.bloques.length === 0 ? (
              <p role="status">Este horario no tiene bloques asociados.</p>
            ) : (
              <div className="table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th scope="col">Fecha</th>
                      <th scope="col">Bloque</th>
                      <th scope="col">Asignatura</th>
                      <th scope="col">Actividad</th>
                      <th scope="col">Complejidad</th>
                    </tr>
                  </thead>
                  <tbody>
                    {datos.bloques.map((bloque) => (
                      <tr key={`${bloque.bloqueId}-${bloque.fecha}`}>
                        <td>{bloque.fecha}</td>
                        <td>
                          <Link to={`/bloques/${bloque.bloqueId}`}>{bloque.descripcion}</Link>
                        </td>
                        <td>{nombreDe(catalogos.asignaturas, bloque.asignaturaId)}</td>
                        <td>{etiquetaTipoActividad(bloque.tipoActividad)}</td>
                        <td>{etiquetaNivelComplejidad(bloque.nivelComplejidad)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}
      </QueryContent>
    </section>
  );
}
