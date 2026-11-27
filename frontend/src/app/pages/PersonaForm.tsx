import { useState, type FormEvent } from "react";
import {
  actualizarPerfil,
  actualizarPersona,
  crearPersona,
  type Perfil,
  type Persona,
  type PersonaInput,
  type TipoPerfil,
} from "../../api/persons";
import { useApiMutation } from "../../api/useApiMutation";
import { ErrorMessage } from "../components/ErrorMessage";
import { TIPO_LABEL } from "./personaLabels";

interface PersonaFormProps {
  /** Persona a editar. Si no se indica, el formulario crea una nueva. */
  persona?: Persona;
  /** Coordinadores disponibles para asignar a un estudiante. */
  coordinadores: Persona[];
  onSaved: () => void;
  onCancel: () => void;
}

type FieldErrors = Partial<
  Record<"nombre" | "tipo" | "edad" | "grupo" | "coordinadorId" | "especialidad", string>
>;

const TIPOS: TipoPerfil[] = ["estudiante", "coordinador", "jefeDepartamento"];

/** Compara dos perfiles del mismo tipo campo por campo. */
function mismoPerfil(a: Perfil, b: Perfil): boolean {
  return JSON.stringify(a) === JSON.stringify(b);
}

/**
 * Formulario de creación o edición de personas. Los campos del perfil dependen
 * del tipo elegido; al editar, el tipo queda fijo.
 */
export function PersonaForm({ persona, coordinadores, onSaved, onCancel }: PersonaFormProps) {
  const original = persona?.perfil;
  const [nombre, setNombre] = useState(persona?.nombre ?? "");
  const [tipo, setTipo] = useState<TipoPerfil | "">(original?.tipo ?? "");
  const [edad, setEdad] = useState(original?.tipo === "estudiante" ? String(original.edad) : "");
  const [grupo, setGrupo] = useState(original?.tipo === "estudiante" ? original.grupo : "");
  const [coordinadorId, setCoordinadorId] = useState(
    original?.tipo === "estudiante" ? original.coordinadorId : "",
  );
  const [especialidad, setEspecialidad] = useState(
    original?.tipo === "coordinador" ? original.especialidad : "",
  );
  const [errors, setErrors] = useState<FieldErrors>({});

  const guardar = useApiMutation(async (input: PersonaInput) => {
    if (!persona) return crearPersona(input);

    let resultado: Persona = persona;
    if (input.nombre !== persona.nombre) {
      resultado = await actualizarPersona(persona.id, { nombre: input.nombre });
    }
    if (!mismoPerfil(input.perfil, persona.perfil)) {
      resultado = await actualizarPerfil(persona.id, input.perfil);
    }
    return resultado;
  });

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const nuevos: FieldErrors = {};
    if (!nombre.trim()) nuevos.nombre = "El nombre es obligatorio.";
    if (!tipo) nuevos.tipo = "Selecciona un perfil.";

    let perfil: Perfil | null = null;
    if (tipo === "estudiante") {
      const edadNumero = Number(edad);
      if (!Number.isInteger(edadNumero) || edadNumero <= 0) {
        nuevos.edad = "La edad debe ser un número entero mayor que cero.";
      }
      if (!grupo.trim()) nuevos.grupo = "El grupo es obligatorio.";
      if (!coordinadorId) nuevos.coordinadorId = "Selecciona un coordinador.";
      perfil = { tipo, edad: edadNumero, grupo: grupo.trim(), coordinadorId };
    } else if (tipo === "coordinador") {
      if (!especialidad.trim()) nuevos.especialidad = "La especialidad es obligatoria.";
      perfil = { tipo, especialidad: especialidad.trim() };
    } else if (tipo === "jefeDepartamento") {
      perfil = { tipo };
    }

    setErrors(nuevos);
    if (Object.keys(nuevos).length > 0 || !perfil) return;

    const ok = await guardar.run({ nombre: nombre.trim(), perfil });
    if (ok.ok) onSaved();
  }

  return (
    <form className="module-form" onSubmit={handleSubmit} noValidate>
      <label>
        Nombre
        <input
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          aria-invalid={errors.nombre ? true : undefined}
        />
      </label>
      {errors.nombre && <p className="field-error">{errors.nombre}</p>}

      <label>
        Perfil
        <select
          value={tipo}
          disabled={persona !== undefined}
          onChange={(e) => {
            setTipo(e.target.value as TipoPerfil | "");
            setErrors({});
          }}
          aria-invalid={errors.tipo ? true : undefined}
        >
          <option value="">Selecciona un perfil</option>
          {TIPOS.map((t) => (
            <option key={t} value={t}>
              {TIPO_LABEL[t]}
            </option>
          ))}
        </select>
      </label>
      {errors.tipo && <p className="field-error">{errors.tipo}</p>}

      {tipo === "estudiante" && (
        <>
          <label>
            Edad
            <input
              type="number"
              min={1}
              value={edad}
              onChange={(e) => setEdad(e.target.value)}
              aria-invalid={errors.edad ? true : undefined}
            />
          </label>
          {errors.edad && <p className="field-error">{errors.edad}</p>}

          <label>
            Grupo
            <input
              value={grupo}
              onChange={(e) => setGrupo(e.target.value)}
              aria-invalid={errors.grupo ? true : undefined}
            />
          </label>
          {errors.grupo && <p className="field-error">{errors.grupo}</p>}

          <label>
            Coordinador asignado
            <select
              value={coordinadorId}
              onChange={(e) => setCoordinadorId(e.target.value)}
              aria-invalid={errors.coordinadorId ? true : undefined}
            >
              <option value="">Selecciona un coordinador</option>
              {coordinadores.map((c) => (
                <option key={c.id} value={c.id} disabled={c.activo === false}>
                  {c.nombre}
                  {c.activo === false ? " (inactivo)" : ""}
                </option>
              ))}
            </select>
          </label>
          {errors.coordinadorId && <p className="field-error">{errors.coordinadorId}</p>}
          {coordinadores.length === 0 && (
            <p className="notice">Primero crea un coordinador para poder asignarlo.</p>
          )}
        </>
      )}

      {tipo === "coordinador" && (
        <>
          <label>
            Especialidad
            <input
              value={especialidad}
              onChange={(e) => setEspecialidad(e.target.value)}
              aria-invalid={errors.especialidad ? true : undefined}
            />
          </label>
          {errors.especialidad && <p className="field-error">{errors.especialidad}</p>}
        </>
      )}

      {guardar.state.status === "error" && <ErrorMessage error={guardar.state.error} />}

      <div className="form-actions">
        <button type="button" className="btn" onClick={onCancel}>
          Cancelar
        </button>
        <button
          type="submit"
          className="btn btn-primary"
          disabled={guardar.state.status === "loading"}
        >
          {guardar.state.status === "loading" ? "Guardando…" : "Guardar persona"}
        </button>
      </div>
    </form>
  );
}
