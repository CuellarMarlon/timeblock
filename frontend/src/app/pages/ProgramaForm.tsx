import { useState, type FormEvent } from "react";
import { actualizarPrograma, crearPrograma, type Programa } from "../../api/program";
import { useApiMutation } from "../../api/useApiMutation";
import { ErrorMessage } from "../components/ErrorMessage";

interface ProgramaFormProps {
  /** Programa a editar. Si no se indica, el formulario crea uno nuevo. */
  programa?: Programa;
  /** Se llama tras guardar con éxito. */
  onSaved: () => void;
  /** Se llama al cancelar el formulario. */
  onCancel: () => void;
}

interface ProgramaFormValues {
  nombre: string;
  planEstudio: string;
}

/** Formulario de creación o edición de programas con validación y errores de la API. */
export function ProgramaForm({ programa, onSaved, onCancel }: ProgramaFormProps) {
  const [nombre, setNombre] = useState(programa?.nombre ?? "");
  const [planEstudio, setPlanEstudio] = useState(programa?.planEstudio ?? "");
  const [errors, setErrors] = useState<{ nombre?: string; planEstudio?: string }>({});

  const guardar = useApiMutation(
    programa
      ? (values: ProgramaFormValues) => actualizarPrograma(programa.id, values)
      : crearPrograma,
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const nuevosErrores: typeof errors = {};
    if (!nombre.trim()) nuevosErrores.nombre = "El nombre es obligatorio.";
    if (!planEstudio.trim()) nuevosErrores.planEstudio = "El plan de estudio es obligatorio.";
    setErrors(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) return;

    const ok = await guardar.submit({ nombre: nombre.trim(), planEstudio: planEstudio.trim() });
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
        Plan de estudio
        <input
          value={planEstudio}
          onChange={(e) => setPlanEstudio(e.target.value)}
          aria-invalid={errors.planEstudio ? true : undefined}
        />
      </label>
      {errors.planEstudio && <p className="field-error">{errors.planEstudio}</p>}

      {guardar.status === "error" && <ErrorMessage error={guardar.error} />}

      <div className="form-actions">
        <button type="button" className="btn" onClick={onCancel}>
          Cancelar
        </button>
        <button type="submit" className="btn btn-primary" disabled={guardar.status === "pending"}>
          {guardar.status === "pending" ? "Guardando…" : "Guardar programa"}
        </button>
      </div>
    </form>
  );
}
