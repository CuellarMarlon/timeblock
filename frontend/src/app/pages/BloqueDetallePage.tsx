import { useCallback, useId, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import {
  asociarBloqueAPrograma,
  desasociarBloqueDePrograma,
  etiquetaNivelComplejidad,
  etiquetaTipoActividad,
  obtenerBloque,
  type Bloque,
} from "../../api/bloques";
import { CATALOGOS_VACIOS, cargarCatalogos, nombreDe, type Catalogos } from "../../api/catalogos";
import { useApiMutation } from "../../api/useApiMutation";
import { useApiQuery } from "../../api/useApiQuery";
import { ErrorMessage } from "../components/ErrorMessage";
import { Modal } from "../components/Modal";
import { QueryContent } from "../components/QueryContent";
import { SelectField } from "../components/SelectField";
import { SuccessMessage } from "../components/SuccessMessage";
import "./BloquesHorarios.css";
import "./ProgramasPage.css";

// Muestra un bloque y deja asociarlo a más programas
export function BloqueDetallePage() {
  const headingId = useId();
  const { bloqueId = "" } = useParams();
  const location = useLocation();
  // Aviso que manda el formulario después de crear o editar el bloque
  const avisoInicial = (location.state as { aviso?: string } | null)?.aviso;

  const catalogosQuery = useApiQuery(cargarCatalogos);
  const catalogos = catalogosQuery.status === "success" ? catalogosQuery.data : CATALOGOS_VACIOS;

  const load = useCallback((signal: AbortSignal) => obtenerBloque(bloqueId, signal), [bloqueId]);
  const bloque = useApiQuery(load);

  // La mutación se queda aquí y no en el detalle: al recargar el bloque el detalle
  // se vuelve a montar y se perdería el aviso de éxito
  const asociar = useApiMutation(asociarBloqueAPrograma);
  const desvincular = useApiMutation(desasociarBloqueDePrograma);
  const [asociado, setAsociado] = useState<string | null>(null);
  const [desvinculado, setDesvinculado] = useState<string | null>(null);
  const [pendiente, setPendiente] = useState<{ id: string; nombre: string } | null>(null);
  const desvinculando = desvincular.state.status === "loading";

  async function asociarPrograma(programaId: string) {
    setAsociado(null);
    setDesvinculado(null);
    const resultado = await asociar.run(bloqueId, programaId);
    if (resultado.ok) {
      setAsociado(nombreDe(catalogos.programas, programaId));
      bloque.reload();
    }
  }

  function solicitarDesvinculacion(programaId: string) {
    setAsociado(null);
    setDesvinculado(null);
    asociar.reset();
    desvincular.reset();
    setPendiente({ id: programaId, nombre: nombreDe(catalogos.programas, programaId) });
  }

  function cerrarConfirmacion() {
    if (desvinculando) return;
    setPendiente(null);
    desvincular.reset();
  }

  async function confirmarDesvinculacion() {
    if (!pendiente) return;
    const resultado = await desvincular.run(bloqueId, pendiente.id);
    if (resultado.ok) {
      setDesvinculado(pendiente.nombre);
      setPendiente(null);
      bloque.reload();
    }
  }

  return (
    <section className="module" aria-labelledby={headingId}>
      <header className="page-header">
        <div>
          <h1 id={headingId}>Detalle del bloque</h1>
          <p>Consulta los datos del bloque y los programas donde se puede utilizar.</p>
        </div>
        <div className="header-actions">
          <Link className="btn" to="/bloques">
            ← Volver a los bloques
          </Link>
          <Link className="btn btn-primary" to={`/bloques/${bloqueId}/editar`}>
            Editar
          </Link>
        </div>
      </header>

      {avisoInicial && <SuccessMessage message={avisoInicial} />}
      {asociado && <SuccessMessage message={`Bloque asociado al programa «${asociado}».`} />}
      {desvinculado && (
        <SuccessMessage message={`Bloque desvinculado del programa «${desvinculado}».`} />
      )}
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
            onDesvincular={solicitarDesvinculacion}
          />
        )}
      </QueryContent>
      {pendiente && (
        <Modal title="Confirmar desvinculación" onClose={cerrarConfirmacion}>
          <p>
            ¿Deseas desvincular este bloque del programa <strong>{pendiente.nombre}</strong>?
          </p>
          {desvincular.state.status === "error" && <ErrorMessage error={desvincular.state.error} />}
          <div className="form-actions">
            <button
              type="button"
              className="btn"
              onClick={cerrarConfirmacion}
              disabled={desvinculando}
            >
              Cancelar
            </button>
            <button
              type="button"
              className="btn btn-primary"
              onClick={confirmarDesvinculacion}
              disabled={desvinculando}
            >
              {desvinculando ? "Desvinculando…" : "Confirmar desvinculación"}
            </button>
          </div>
        </Modal>
      )}
    </section>
  );
}

interface BloqueDetalleProps {
  bloque: Bloque;
  catalogos: Catalogos;
  asociando: boolean;
  onAsociar: (programaId: string) => void;
  onDesvincular: (programaId: string) => void;
}

function BloqueDetalle({
  bloque,
  catalogos,
  asociando,
  onAsociar,
  onDesvincular,
}: BloqueDetalleProps) {
  const [programaId, setProgramaId] = useState("");
  const disponibles = catalogos.programas.filter((p) => !bloque.programasIds.includes(p.id));

  return (
    <>
      <dl className="card detail-list">
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

      <section className="module-panel" aria-labelledby="bloque-programas-titulo">
        <h2 id="bloque-programas-titulo">Programas asociados</h2>

        {bloque.programasIds.length === 0 ? (
          <p>Este bloque todavía no está asociado a ningún programa.</p>
        ) : (
          <ul className="program-list">
            {bloque.programasIds.map((id) => (
              <li key={id}>
                <span className="badge badge--coordinador">
                  {nombreDe(catalogos.programas, id)}
                </span>
                <button
                  type="button"
                  className="btn"
                  aria-label={`Desvincular de ${nombreDe(catalogos.programas, id)}`}
                  onClick={() => onDesvincular(id)}
                >
                  Desvincular
                </button>
              </li>
            ))}
          </ul>
        )}

        {disponibles.length > 0 ? (
          <form
            className="card associate-form associate-form--simple"
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
            <button type="submit" className="btn btn-primary" disabled={!programaId || asociando}>
              {asociando ? "Asociando…" : "Asociar"}
            </button>
          </form>
        ) : (
          catalogos.programas.length > 0 && <p>El bloque ya está asociado a todos los programas.</p>
        )}
      </section>
    </>
  );
}
