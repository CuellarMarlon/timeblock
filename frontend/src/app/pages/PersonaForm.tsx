import { useState, type FormEvent } from "react";
import { crearPersona, type Perfil, type Persona, type TipoPerfil } from "../../api/persons";
import { useApiMutation } from "../../api/useApiMutation";
import { ErrorMessage } from "../components/ErrorMessage";
import { TIPO_LABEL } from "./personaLabels";

interface PersonaFormProps {
  /** Coordinadores disponibles para asignar a un estudiante. */
  coordinadores: Persona[];
  onSaved: () => void;
  onCancel: () => void;
}

type FieldErrors = Partial<
  Record<"nombre" | "tipo" | "edad" | "grupo" | "coordinadorId" | "especialidad", string>
>;

const TIPOS: TipoPerfil[] = ["estudiante", "coordinador", "jefeDepartamento"];

/** Formulario de creación de personas: los campos del perfil dependen del tipo elegido. */
export function PersonaForm({ coordinadores, onSaved, onCancel }: PersonaFormProps) {
  const [nombre, setNombre] = useState("");
  const [tipo, setTipo] = useState<TipoPerfil | "">("");
  const [edad, setEdad] = useState("");
  const [grupo, setGrupo] = useState("");
  const [coordinadorId, setCoordinadorId] = useState("");
  const [especialidad, setEspecialidad] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});
  const crear = useApiMutation(crearPersona);

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

    const ok = await crear.submit({ nombre: nombre.trim(), perfil });
    if (ok) onSaved();
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
                <option key={c.id} value={c.id}>
                  {c.nombre}
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

      {crear.status === "error" && <ErrorMessage error={crear.error} />}

      <div className="form-actions">
        <button type="button" className="btn" onClick={onCancel}>
          Cancelar
        </button>
        <button type="submit" className="btn btn-primary" disabled={crear.status === "pending"}>
          {crear.status === "pending" ? "Guardando…" : "Guardar persona"}
        </button>
      </div>
    </form>
  );
}
