import { useCallback, useState, type FormEvent } from "react";
import {
  asociarAsignatura,
  desasociarAsignatura,
  listarAsignaturas,
  listarAsignaturasDePrograma,
  listarProgramas,
} from "../../api/program";
import { useApiMutation } from "../../api/useApiMutation";
import { useApiQuery } from "../../api/useApiQuery";
import { ErrorMessage } from "../components/ErrorMessage";
import { Modal } from "../components/Modal";
import { QueryContent } from "../components/QueryContent";

/** Máximo de elementos por página admitido por la API. */
const MAX_PAGE_SIZE = 100;

interface AsociacionInput {
  programaId: string;
  asignaturaId: string;
}

/** Acción de asociación, definida fuera del componente para que sea estable. */
function asociar({ programaId, asignaturaId }: AsociacionInput) {
  return asociarAsignatura(programaId, asignaturaId);
}
interface AsociarAsignaturaProps {
  /** Cambia cada vez que se crea o edita una asignatura, para recargar el selector. */
  refreshKey?: number;
}

export function AsociarAsignatura({ refreshKey = 0 }: AsociarAsignaturaProps) {
  /* Sección "Asignaturas por programa": permite asociar una asignatura a un
   * programa y consultar las asignaturas ya asociadas al programa elegido.
   */
  const [programaId, setProgramaId] = useState("");
  const [asignaturaId, setAsignaturaId] = useState("");
  const [errors, setErrors] = useState<{ programa?: string; asignatura?: string }>({});
  const [notice, setNotice] = useState<string | null>(null);
  const [pendiente, setPendiente] = useState<{
    programaId: string;
    programa: string;
    asignaturaId: string;
    asignatura: string;
  } | null>(null);
  const mutation = useApiMutation(asociar);
  const baja = useApiMutation(desasociarAsignatura);
  const eliminando = baja.state.status === "loading";

  const loadProgramas = useCallback(
    (signal: AbortSignal) => listarProgramas({ page: 1, pageSize: MAX_PAGE_SIZE }, signal),
    [],
  );
  const loadAsignaturas = useCallback(
    (signal: AbortSignal) => {
      void refreshKey;
      return listarAsignaturas({ page: 1, pageSize: MAX_PAGE_SIZE }, signal);
    },
    [refreshKey],
  );
  const programas = useApiQuery(loadProgramas);
  const asignaturas = useApiQuery(loadAsignaturas);

  const loadAsociadas = useCallback(
    (signal: AbortSignal) =>
      programaId
        ? listarAsignaturasDePrograma(programaId, { page: 1, pageSize: MAX_PAGE_SIZE }, signal)
        : Promise.resolve(null),
    [programaId],
  );
  const asociadas = useApiQuery(loadAsociadas);

  function cerrarConfirmacion() {
    if (eliminando) return;
    setPendiente(null);
    baja.reset();
  }

  async function confirmarDesvinculacion() {
    if (!pendiente) return;
    const resultado = await baja.run(pendiente.programaId, pendiente.asignaturaId);
    if (resultado.ok) {
      setPendiente(null);
      setNotice("Asignatura desvinculada correctamente.");
      asociadas.reload();
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setNotice(null);

    const nuevos: typeof errors = {};
    if (!programaId) nuevos.programa = "Selecciona un programa.";
    if (!asignaturaId) nuevos.asignatura = "Selecciona una asignatura.";
    setErrors(nuevos);
    if (Object.keys(nuevos).length > 0) return;

    const ok = await mutation.run({ programaId, asignaturaId });
    if (ok.ok) {
      setAsignaturaId("");
      setNotice("Asignatura asociada correctamente.");
      asociadas.reload();
    }
  }

  const nombrePrograma = (id: string) =>
    programas.status === "success"
      ? (() => {
          const programa = programas.data.items.find((p) => p.id === id);
          return programa
            ? `${programa.nombre}${programa.activo === false ? " (inactivo)" : ""}`
            : id;
        })()
      : "";

  return (
    <section className="module-panel" aria-labelledby="asociar-titulo">
      <h2 id="asociar-titulo">Asignaturas por programa</h2>

      <QueryContent
        query={programas}
        onRetry={programas.reload}
        loadingLabel="Cargando programas…"
        emptyMessage="Crea al menos un programa para poder asociar asignaturas."
      >
        {(dataProgramas) => (
          <QueryContent
            query={asignaturas}
            onRetry={asignaturas.reload}
            loadingLabel="Cargando asignaturas…"
            emptyMessage="Crea al menos una asignatura para poder asociarla."
          >
            {(dataAsignaturas) => (
              <form className="card associate-form" onSubmit={handleSubmit} noValidate>
                <label>
                  Programa académico
                  <select
                    value={programaId}
                    onChange={(e) => {
                      setProgramaId(e.target.value);
                      setNotice(null);
                      mutation.reset();
                    }}
                    aria-invalid={errors.programa ? true : undefined}
                  >
                    <option value="">Selecciona un programa</option>
                    {dataProgramas.items.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.nombre}
                        {p.activo === false ? " (inactivo)" : ""}
                      </option>
                    ))}
                  </select>
                  {errors.programa && <span className="field-error">{errors.programa}</span>}
                </label>

                <label>
                  Asignatura
                  <select
                    value={asignaturaId}
                    onChange={(e) => setAsignaturaId(e.target.value)}
                    aria-invalid={errors.asignatura ? true : undefined}
                  >
                    <option value="">Selecciona una asignatura</option>
                    {dataAsignaturas.items.map((a) => (
                      <option key={a.id} value={a.id} disabled={a.activo === false}>
                        {a.nombre}
                        {a.activo === false ? " (inactivo)" : ""}
                      </option>
                    ))}
                  </select>
                  {errors.asignatura && <span className="field-error">{errors.asignatura}</span>}
                </label>

                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={
                    mutation.state.status === "loading" ||
                    dataProgramas.items.some((p) => p.id === programaId && p.activo === false)
                  }
                >
                  {mutation.state.status === "loading" ? "Asociando…" : "Asociar"}
                </button>
              </form>
            )}
          </QueryContent>
        )}
      </QueryContent>

      {notice && (
        <p className="notice" role="status">
          {notice}
        </p>
      )}
      {mutation.state.status === "error" && <ErrorMessage error={mutation.state.error} />}

      {programaId && (
        <QueryContent
          query={asociadas}
          onRetry={asociadas.reload}
          loadingLabel="Cargando asignaturas asociadas…"
          emptyMessage="Este programa todavía no tiene asignaturas asociadas."
        >
          {(data) =>
            data && (
              <div className="card">
                <table className="table">
                  <caption className="visually-hidden">
                    Asignaturas asociadas a {nombrePrograma(programaId)}
                  </caption>
                  <thead>
                    <tr>
                      <th scope="col">Programa académico</th>
                      <th scope="col">Asignatura</th>
                      <th scope="col">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.items.map((a) => (
                      <tr key={a.id}>
                        <td>{nombrePrograma(programaId)}</td>
                        <td>
                          {a.nombre}
                          {a.activo === false ? " (inactiva)" : ""}
                        </td>
                        <td>
                          <button
                            type="button"
                            className="btn"
                            aria-label={`Desvincular ${a.nombre} de ${nombrePrograma(programaId)}`}
                            onClick={() => {
                              setNotice(null);
                              baja.reset();
                              setPendiente({
                                programaId,
                                programa: nombrePrograma(programaId),
                                asignaturaId: a.id,
                                asignatura: a.nombre,
                              });
                            }}
                          >
                            Desvincular
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )
          }
        </QueryContent>
      )}
      {pendiente && (
        <Modal title="Confirmar desvinculación" onClose={cerrarConfirmacion}>
          <p>
            ¿Deseas desvincular <strong>{pendiente.asignatura}</strong> del programa{" "}
            <strong>{pendiente.programa}</strong>?
          </p>
          {baja.state.status === "error" && <ErrorMessage error={baja.state.error} />}
          <div className="form-actions">
            <button
              type="button"
              className="btn"
              onClick={cerrarConfirmacion}
              disabled={eliminando}
            >
              Cancelar
            </button>
            <button
              type="button"
              className="btn btn-primary"
              onClick={confirmarDesvinculacion}
              disabled={eliminando}
            >
              {eliminando ? "Desvinculando…" : "Confirmar desvinculación"}
            </button>
          </div>
        </Modal>
      )}
    </section>
  );
}
