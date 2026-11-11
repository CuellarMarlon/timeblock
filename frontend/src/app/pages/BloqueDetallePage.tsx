import { useCallback, useId, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import {
  asociarBloqueAPrograma,
  etiquetaNivelComplejidad,
  etiquetaTipoActividad,
  obtenerBloque,
  type Bloque,
} from "../../api/bloques";
import { CATALOGOS_VACIOS, cargarCatalogos, nombreDe, type Catalogos } from "../../api/catalogos";
import { useApiMutation } from "../../api/useApiMutation";
import { useApiQuery } from "../../api/useApiQuery";
import { ErrorMessage } from "../components/ErrorMessage";
import { QueryContent } from "../components/QueryContent";
import { SelectField } from "../components/SelectField";
import { SuccessMessage } from "../components/SuccessMessage";
import "../components/ui.css";

/** Consulta de un bloque y asociación a programas adicionales. */
export function BloqueDetallePage() {
  const headingId = useId();
  const { bloqueId = "" } = useParams();
  const location = useLocation();
  // Aviso enviado por el formulario tras crear o editar el bloque.
  const avisoInicial = (location.state as { aviso?: string } | null)?.aviso;

  const catalogosQuery = useApiQuery(cargarCatalogos);
  const catalogos = catalogosQuery.status === "success" ? catalogosQuery.data : CATALOGOS_VACIOS;

  const load = useCallback((signal: AbortSignal) => obtenerBloque(bloqueId, signal), [bloqueId]);
  const bloque = useApiQuery(load);

  // La mutación vive aquí (y no en la sección) porque al recargar el bloque
  // la sección se desmonta y perdería el aviso de éxito.
  const asociar = useApiMutation(asociarBloqueAPrograma);
  const [asociado, setAsociado] = useState<string | null>(null);

  async function asociarPrograma(programaId: string) {
    setAsociado(null);
    const resultado = await asociar.run(bloqueId, programaId);
    if (resultado.ok) {
      setAsociado(nombreDe(catalogos.programas, programaId));
      bloque.reload();
    }
  }

  return (
    <section aria-labelledby={headingId}>
      <div className="page-header">
        <h1 id={headingId}>Detalle del bloque</h1>
        <Link className="button-link" to={`/bloques/${bloqueId}/editar`}>
          Editar
        </Link>
      </div>
      <p>
        <Link to="/bloques">← Volver a los bloques</Link>
      </p>

      {avisoInicial && <SuccessMessage message={avisoInicial} />}
      {asociado && <SuccessMessage message={`Bloque asociado al programa «${asociado}».`} />}
      {asociar.state.status === "error" && <ErrorMessage error={asociar.state.error} />}
      {catalogosQuery.status === "error" && (
        <ErrorMessage error={catalogosQuery.error} onRetry={catalogosQuery.reload} />
      )}

      <QueryContent
        query={bloque}
        onRetry={bloque.reload}
        loadingLabel="Cargando bloque…"
        isEmpty={() => false}
      >
        {(datos) => (
          <BloqueDetalle
            bloque={datos}
            catalogos={catalogos}
            asociando={asociar.state.status === "loading"}
            onAsociar={asociarPrograma}
          />
        )}
      </QueryContent>
    </section>
  );
}

interface BloqueDetalleProps {
  bloque: Bloque;
  catalogos: Catalogos;
  asociando: boolean;
  onAsociar: (programaId: string) => void;
}

function BloqueDetalle({ bloque, catalogos, asociando, onAsociar }: BloqueDetalleProps) {
  const [programaId, setProgramaId] = useState("");
  const disponibles = catalogos.programas.filter((p) => !bloque.programasIds.includes(p.id));

  return (
    <>
      <dl className="detail-list">
        <dt>Descripción</dt>
        <dd>{bloque.descripcion}</dd>
        <dt>Asignatura</dt>
        <dd>{nombreDe(catalogos.asignaturas, bloque.asignaturaId)}</dd>
        <dt>Tipo de actividad</dt>
        <dd>{etiquetaTipoActividad(bloque.tipoActividad)}</dd>
        <dt>Complejidad</dt>
        <dd>{etiquetaNivelComplejidad(bloque.nivelComplejidad)}</dd>
        <dt>Coordinador creador</dt>
        <dd>{nombreDe(catalogos.coordinadores, bloque.coordinadorId)}</dd>
      </dl>

      <h2>Programas asociados</h2>
      {bloque.programasIds.length === 0 ? (
        <p>Este bloque todavía no está asociado a ningún programa.</p>
      ) : (
        <ul>
          {bloque.programasIds.map((id) => (
            <li key={id}>{nombreDe(catalogos.programas, id)}</li>
          ))}
        </ul>
      )}

      {disponibles.length > 0 ? (
        <form
          className="inline-form"
          aria-label="Asociar a un programa"
          onSubmit={(event) => {
            event.preventDefault();
            if (programaId) {
              onAsociar(programaId);
              setProgramaId("");
            }
          }}
        >
          <SelectField
            label="Asociar a un programa"
            placeholder="Selecciona un programa"
            value={programaId}
            options={disponibles.map((p) => ({ value: p.id, label: p.nombre }))}
            onChange={setProgramaId}
          />
          <button type="submit" disabled={!programaId || asociando}>
            {asociando ? "Asociando…" : "Asociar"}
          </button>
        </form>
      ) : (
        catalogos.programas.length > 0 && <p>El bloque ya está asociado a todos los programas.</p>
      )}
    </>
  );
}
