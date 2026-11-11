import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  actualizarBloque,
  crearBloque,
  NIVELES_COMPLEJIDAD,
  TIPOS_ACTIVIDAD,
  type ActualizarBloque,
  type Bloque,
} from "../../api/bloques";
import { nombreDe, type Catalogos } from "../../api/catalogos";
import { useApiMutation } from "../../api/useApiMutation";
import { ErrorMessage } from "../components/ErrorMessage";
import { SelectField } from "../components/SelectField";
import "../components/ui.css";

interface BloqueFormProps {
  catalogos: Catalogos;
  /** Si se indica, el formulario edita ese bloque; si no, crea uno nuevo. */
  bloque?: Bloque;
}

type Campo =
  "descripcion" | "asignaturaId" | "tipoActividad" | "nivelComplejidad" | "coordinadorId";
type Errores = Partial<Record<Campo, string>>;

/**
 * Formulario para crear o editar un bloque. Al crear permite elegir el
 * coordinador y los programas iniciales; al editar solo se envían los campos
 * que cambiaron, porque la API exige al menos uno.
 */
export function BloqueForm({ catalogos, bloque }: BloqueFormProps) {
  const navigate = useNavigate();
  const editando = bloque !== undefined;

  const [descripcion, setDescripcion] = useState(bloque?.descripcion ?? "");
  const [asignaturaId, setAsignaturaId] = useState(bloque?.asignaturaId ?? "");
  const [tipoActividad, setTipoActividad] = useState<string>(bloque?.tipoActividad ?? "");
  const [nivelComplejidad, setNivelComplejidad] = useState<string>(bloque?.nivelComplejidad ?? "");
  const [coordinadorId, setCoordinadorId] = useState("");
  const [programasIds, setProgramasIds] = useState<string[]>([]);
  const [errores, setErrores] = useState<Errores>({});
  const [sinCambios, setSinCambios] = useState(false);

  const crear = useApiMutation(crearBloque);
  const actualizar = useApiMutation(actualizarBloque);
  const envio = editando ? actualizar.state : crear.state;
  const enviando = envio.status === "loading";

  function validar(): Errores {
    const nuevos: Errores = {};
    if (!descripcion.trim()) nuevos.descripcion = "La descripción es obligatoria.";
    if (!asignaturaId) nuevos.asignaturaId = "Selecciona una asignatura.";
    if (!tipoActividad) nuevos.tipoActividad = "Selecciona el tipo de actividad.";
    if (!nivelComplejidad) nuevos.nivelComplejidad = "Selecciona la complejidad.";
    if (!editando && !coordinadorId) nuevos.coordinadorId = "Selecciona el coordinador creador.";
    return nuevos;
  }

  async function enviar(event: FormEvent) {
    event.preventDefault();
    setSinCambios(false);

    const nuevos = validar();
    setErrores(nuevos);
    if (Object.keys(nuevos).length > 0) return;

    if (bloque) {
      const cambios = calcularCambios(bloque, {
        descripcion: descripcion.trim(),
        asignaturaId,
        tipoActividad,
        nivelComplejidad,
      });
      if (Object.keys(cambios).length === 0) {
        setSinCambios(true);
        return;
      }
      const resultado = await actualizar.run(bloque.id, cambios);
      if (resultado.ok) {
        navigate(`/bloques/${bloque.id}`, {
          state: { aviso: "Bloque actualizado correctamente." },
        });
      }
      return;
    }

    const resultado = await crear.run({
      descripcion: descripcion.trim(),
      asignaturaId,
      tipoActividad: tipoActividad as Bloque["tipoActividad"],
      nivelComplejidad: nivelComplejidad as Bloque["nivelComplejidad"],
      coordinadorId,
      programasIds,
    });
    if (resultado.ok) {
      navigate(`/bloques/${resultado.data.id}`, {
        state: { aviso: "Bloque creado correctamente." },
      });
    }
  }

  function alternarPrograma(id: string, marcado: boolean) {
    setProgramasIds((actuales) =>
      marcado ? [...actuales, id] : actuales.filter((actual) => actual !== id),
    );
  }

  return (
    <form
      className="form"
      onSubmit={enviar}
      noValidate
      aria-label={editando ? "Editar bloque" : "Nuevo bloque"}
    >
      <div className="field">
        <label htmlFor="bloque-descripcion">Descripción</label>
        <input
          id="bloque-descripcion"
          type="text"
          value={descripcion}
          aria-invalid={errores.descripcion ? true : undefined}
          aria-describedby={errores.descripcion ? "bloque-descripcion-error" : undefined}
          onChange={(event) => setDescripcion(event.target.value)}
        />
        {errores.descripcion && (
          <p className="field-error" id="bloque-descripcion-error">
            {errores.descripcion}
          </p>
        )}
      </div>

      <SelectField
        label="Asignatura"
        placeholder="Selecciona una asignatura"
        value={asignaturaId}
        options={catalogos.asignaturas.map((a) => ({ value: a.id, label: a.nombre }))}
        error={errores.asignaturaId}
        onChange={setAsignaturaId}
      />
      <SelectField
        label="Tipo de actividad"
        placeholder="Selecciona el tipo"
        value={tipoActividad}
        options={TIPOS_ACTIVIDAD}
        error={errores.tipoActividad}
        onChange={setTipoActividad}
      />
      <SelectField
        label="Complejidad"
        placeholder="Selecciona la complejidad"
        value={nivelComplejidad}
        options={NIVELES_COMPLEJIDAD}
        error={errores.nivelComplejidad}
        onChange={setNivelComplejidad}
      />

      {editando ? (
        <p>
          Coordinador creador:{" "}
          <strong>{nombreDe(catalogos.coordinadores, bloque.coordinadorId)}</strong>
        </p>
      ) : (
        <>
          <SelectField
            label="Coordinador creador"
            placeholder="Selecciona un coordinador"
            value={coordinadorId}
            options={catalogos.coordinadores.map((c) => ({ value: c.id, label: c.nombre }))}
            error={errores.coordinadorId}
            onChange={setCoordinadorId}
          />
          <fieldset>
            <legend>Programas donde se puede utilizar (opcional)</legend>
            {catalogos.programas.length === 0 ? (
              <p>No hay programas registrados.</p>
            ) : (
              <ul className="check-list">
                {catalogos.programas.map((programa) => (
                  <li key={programa.id}>
                    <label>
                      <input
                        type="checkbox"
                        checked={programasIds.includes(programa.id)}
                        onChange={(event) => alternarPrograma(programa.id, event.target.checked)}
                      />{" "}
                      {programa.nombre}
                    </label>
                  </li>
                ))}
              </ul>
            )}
          </fieldset>
        </>
      )}

      {sinCambios && (
        <p className="notice" role="status">
          No hay cambios que guardar.
        </p>
      )}
      {envio.status === "error" && <ErrorMessage error={envio.error} />}

      <div className="form-actions">
        <button type="submit" disabled={enviando}>
          {enviando ? "Guardando…" : editando ? "Guardar cambios" : "Crear bloque"}
        </button>
        <Link to={editando ? `/bloques/${bloque.id}` : "/bloques"}>Cancelar</Link>
      </div>
    </form>
  );
}

/** Compara el formulario con el bloque original y devuelve solo lo modificado. */
function calcularCambios(
  original: Bloque,
  nuevo: {
    descripcion: string;
    asignaturaId: string;
    tipoActividad: string;
    nivelComplejidad: string;
  },
): ActualizarBloque {
  const cambios: ActualizarBloque = {};
  if (nuevo.descripcion !== original.descripcion) cambios.descripcion = nuevo.descripcion;
  if (nuevo.asignaturaId !== original.asignaturaId) cambios.asignaturaId = nuevo.asignaturaId;
  if (nuevo.tipoActividad !== original.tipoActividad) {
    cambios.tipoActividad = nuevo.tipoActividad as Bloque["tipoActividad"];
  }
  if (nuevo.nivelComplejidad !== original.nivelComplejidad) {
    cambios.nivelComplejidad = nuevo.nivelComplejidad as Bloque["nivelComplejidad"];
  }
  return cambios;
}
