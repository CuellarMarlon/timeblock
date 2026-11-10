import { useState, type FormEvent } from "react";
import { crearPrograma } from "../../api/program";
import { useApiMutation } from "../../api/useApiMutation";
import { ErrorMessage } from "../components/ErrorMessage";

interface ProgramaFormProps {
  /** Se llama tras crear un programa con éxito. */
  onCreated: () => void;
  /** Se llama al cancelar el formulario. */
  onCancel: () => void;
}

/** Formulario de creación de programas con validación, envío y errores de la API. */
export function ProgramaForm({ onCreated, onCancel }: ProgramaFormProps) {
  const [nombre, setNombre] = useState("");
  const [planEstudio, setPlanEstudio] = useState("");
  const [errors, setErrors] = useState<{ nombre?: string; planEstudio?: string }>({});
  const crear = useApiMutation(crearPrograma);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const nuevosErrores: typeof errors = {};
    if (!nombre.trim()) nuevosErrores.nombre = "El nombre es obligatorio.";
    if (!planEstudio.trim()) nuevosErrores.planEstudio = "El plan de estudio es obligatorio.";
    setErrors(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) return;

    const ok = await crear.submit({ nombre: nombre.trim(), planEstudio: planEstudio.trim() });
    if (ok) onCreated();
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

      {crear.status === "error" && <ErrorMessage error={crear.error} />}

      <div className="form-actions">
        <button type="button" className="btn" onClick={onCancel}>
          Cancelar
        </button>
        <button type="submit" className="btn btn-primary" disabled={crear.status === "pending"}>
          {crear.status === "pending" ? "Guardando…" : "Guardar programa"}
        </button>
      </div>
    </form>
  );
}
