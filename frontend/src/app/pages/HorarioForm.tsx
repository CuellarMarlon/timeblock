import { useCallback, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { etiquetaNivelComplejidad, etiquetaTipoActividad, listarBloques } from "../../api/bloques";
import { MAX_PAGE_SIZE, nombreDe, type Catalogos } from "../../api/catalogos";
import { crearHorario } from "../../api/horarios";
import { useApiMutation } from "../../api/useApiMutation";
import { useApiQuery } from "../../api/useApiQuery";
import { ErrorMessage } from "../components/ErrorMessage";
import { QueryContent } from "../components/QueryContent";
import { SelectField } from "../components/SelectField";
import "./BloquesHorarios.css";
import "./ProgramasPage.css";

// Bloques elegidos: la clave es el id del bloque y el valor su fecha (YYYY-MM-DD)
type Seleccion = Record<string, string>;

interface Errores {
  programaId?: string;
  periodoAcademico?: string;
  coordinadorId?: string;
  bloques?: string;
}

interface HorarioFormProps {
  catalogos: Catalogos;
}

// Formulario para armar un horario a mano: programa, periodo, coordinador y bloques,
// con una fecha obligatoria por cada bloque. No pide ni manda el estado del horario
// ni la fecha de inicio de la autorización, porque las pone el servidor.
// En M4 tampoco se piden hora, periodo de impartición ni aula
export function HorarioForm({ catalogos }: HorarioFormProps) {
  const navigate = useNavigate();
  const [programaId, setProgramaId] = useState("");
  const [periodoAcademico, setPeriodoAcademico] = useState("");
  const [coordinadorId, setCoordinadorId] = useState("");
  const [seleccion, setSeleccion] = useState<Seleccion>({});
  const [errores, setErrores] = useState<Errores>({});
  const [intentado, setIntentado] = useState(false);

  const crear = useApiMutation(crearHorario);
  const enviando = crear.state.status === "loading";

  // Los bloques disponibles dependen del programa, así que al cambiarlo se borra la selección
  function cambiarPrograma(id: string) {
    setProgramaId(id);
    setSeleccion({});
  }

  function validar(): Errores {
    const nuevos: Errores = {};
    if (!programaId) nuevos.programaId = "Selecciona un programa.";
    if (!periodoAcademico.trim()) nuevos.periodoAcademico = "El periodo académico es obligatorio.";
    if (!coordinadorId) nuevos.coordinadorId = "Selecciona el coordinador.";

    const fechas = Object.values(seleccion);
    if (fechas.length === 0) {
      nuevos.bloques = "Selecciona al menos un bloque.";
    } else if (fechas.some((fecha) => !fecha)) {
      nuevos.bloques = "Indica la fecha de cada bloque seleccionado.";
    }
    return nuevos;
  }

  async function enviar(event: FormEvent) {
    event.preventDefault();
    setIntentado(true);

    const nuevos = validar();
    setErrores(nuevos);
    if (Object.keys(nuevos).length > 0) return;

    const resultado = await crear.run({
      programaId,
      periodoAcademico: periodoAcademico.trim(),
      coordinadorId,
      bloques: Object.entries(seleccion).map(([bloqueId, fecha]) => ({ bloqueId, fecha })),
    });
    if (resultado.ok) {
      navigate(`/horarios/${resultado.data.id}`, {
        state: { aviso: "Horario creado correctamente con estado GENERADO." },
      });
    }
  }

  return (
    <form
      className="module-form card form-card"
      onSubmit={enviar}
      noValidate
      aria-label="Nuevo horario"
    >
      <SelectField
        label="Programa"
        placeholder="Selecciona un programa"
        value={programaId}
        options={catalogos.programas
          .filter((p) => p.activo !== false)
          .map((p) => ({ value: p.id, label: p.nombre }))}
        error={errores.programaId}
        onChange={cambiarPrograma}
      />

      <div className="field">
        <label>
          Periodo académico
          <input
            type="text"
            placeholder="2026-2027"
            value={periodoAcademico}
            aria-invalid={errores.periodoAcademico ? true : undefined}
            aria-describedby={errores.periodoAcademico ? "horario-periodo-error" : undefined}
            onChange={(event) => setPeriodoAcademico(event.target.value)}
          />
        </label>
        {errores.periodoAcademico && (
          <p className="field-error" id="horario-periodo-error">
            {errores.periodoAcademico}
          </p>
        )}
      </div>

      <SelectField
        label="Coordinador"
        placeholder="Selecciona un coordinador"
        value={coordinadorId}
        options={catalogos.coordinadores
          .filter((c) => c.activo !== false)
          .map((c) => ({ value: c.id, label: c.nombre }))}
        error={errores.coordinadorId}
        onChange={setCoordinadorId}
      />

      <fieldset className="fieldset--plano">
        <legend>Bloques y fechas</legend>
        {programaId ? (
          <SelectorBloques
            key={programaId}
            programaId={programaId}
            catalogos={catalogos}
            seleccion={seleccion}
            onChange={setSeleccion}
            marcarFaltantes={intentado}
          />
        ) : (
          <p>Selecciona primero un programa para ver sus bloques.</p>
        )}
        {errores.bloques && (
          <p className="field-error" role="alert">
            {errores.bloques}
          </p>
        )}
      </fieldset>

      {crear.state.status === "error" && <ErrorMessage error={crear.state.error} />}

      <div className="form-actions">
        <Link className="btn" to="/horarios">
          Cancelar
        </Link>
        <button type="submit" className="btn btn-primary" disabled={enviando}>
          {enviando ? "Guardando…" : "Crear horario"}
        </button>
      </div>
    </form>
  );
}

interface SelectorBloquesProps {
  programaId: string;
  catalogos: Catalogos;
  seleccion: Seleccion;
  onChange: (seleccion: Seleccion) => void;
  // Si es true, marca los bloques elegidos a los que todavía les falta la fecha
  marcarFaltantes: boolean;
}

// Lista los bloques del programa y pide una fecha por cada bloque marcado
function SelectorBloques({
  programaId,
  catalogos,
  seleccion,
  onChange,
  marcarFaltantes,
}: SelectorBloquesProps) {
  const load = useCallback(
    (signal: AbortSignal) =>
      listarBloques({ programaId }, { page: 1, pageSize: MAX_PAGE_SIZE }, signal),
    [programaId],
  );
  const bloques = useApiQuery(load);
  const cantidad = Object.keys(seleccion).length;

  function alternar(bloqueId: string, marcado: boolean) {
    if (marcado) {
      onChange({ ...seleccion, [bloqueId]: "" });
    } else {
      const resto = { ...seleccion };
      delete resto[bloqueId];
      onChange(resto);
    }
  }

  return (
    <QueryContent
      query={bloques}
      onRetry={bloques.reload}
      loadingLabel="Cargando bloques del programa…"
      emptyMessage="Este programa no tiene bloques asociados. Asócialos desde Bloques de contenido."
    >
      {(pagina) => (
        <>
          {pagina.totalItems > pagina.items.length && (
            <p className="notice" role="status">
              Se muestran los primeros {pagina.items.length} de {pagina.totalItems} bloques.
            </p>
          )}
          <div className="card">
            <table className="table table--seleccion">
              <thead>
                <tr>
                  <th scope="col">Incluir</th>
                  <th scope="col">Bloque</th>
                  <th scope="col">Asignatura</th>
                  <th scope="col">Actividad</th>
                  <th scope="col">Complejidad</th>
                  <th scope="col">Fecha</th>
                </tr>
              </thead>
              <tbody>
                {pagina.items
                  .filter((bloque) => bloque.activo !== false)
                  .map((bloque) => {
                    const marcado = bloque.id in seleccion;
                    const fecha = seleccion[bloque.id] ?? "";
                    const falta = marcarFaltantes && marcado && !fecha;
                    return (
                      <tr key={bloque.id}>
                        <td>
                          <input
                            type="checkbox"
                            checked={marcado}
                            aria-label={`Incluir ${bloque.descripcion}`}
                            onChange={(event) => alternar(bloque.id, event.target.checked)}
                          />
                        </td>
                        <td>{bloque.descripcion}</td>
                        <td>{nombreDe(catalogos.asignaturas, bloque.asignaturaId)}</td>
                        <td>{etiquetaTipoActividad(bloque.tipoActividad)}</td>
                        <td>{etiquetaNivelComplejidad(bloque.nivelComplejidad)}</td>
                        <td>
                          <input
                            type="date"
                            value={fecha}
                            disabled={!marcado}
                            aria-label={`Fecha de ${bloque.descripcion}`}
                            aria-invalid={falta ? true : undefined}
                            onChange={(event) =>
                              onChange({ ...seleccion, [bloque.id]: event.target.value })
                            }
                          />
                          {falta && <p className="field-error">Falta la fecha.</p>}
                        </td>
                      </tr>
                    );
                  })}
              </tbody>
            </table>
          </div>
          <p>
            {cantidad} {cantidad === 1 ? "bloque seleccionado" : "bloques seleccionados"}
          </p>
        </>
      )}
    </QueryContent>
  );
}
