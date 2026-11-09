import { useState, type FormEvent } from "react";
import { crearPrograma } from "../../api/program";
import { useApiMutation } from "../../api/useApiMutation";
import { ErrorMessage } from "../components/ErrorMessage";

interface ProgramaFormProps {
  /** Se llama tras crear un programa con éxito (por ejemplo, para recargar el listado). */
  onCreated: () => void;
}

/** Formulario de creación de programas con validación, envío y estados de la API. */
export function ProgramaForm({ onCreated }: ProgramaFormProps) {
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
    if (ok) {
      setNombre("");
      setPlanEstudio("");
      onCreated();
    }
  }

  return (
    <form className="module-form" onSubmit={handleSubmit} noValidate>
      <h2>Nuevo programa</h2>

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

      <button type="submit" disabled={crear.status === "pending"}>
        {crear.status === "pending" ? "Guardando…" : "Guardar programa"}
      </button>

      {crear.status === "success" && <p role="status">Programa creado correctamente.</p>}
      {crear.status === "error" && <ErrorMessage error={crear.error} />}
    </form>
  );
}
