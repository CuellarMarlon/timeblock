import { useState, type FormEvent } from "react";
import { actualizarAsignatura, crearAsignatura, type Asignatura } from "../../api/program";
import { useApiMutation } from "../../api/useApiMutation";
import { ErrorMessage } from "../components/ErrorMessage";

interface AsignaturaFormProps {
  /** Asignatura a editar. Si no se indica, el formulario crea una nueva. */
  asignatura?: Asignatura;
  onSaved: () => void;
  onCancel: () => void;
}

interface AsignaturaFormValues {
  nombre: string;
}

/** Formulario de creación o edición de asignaturas. */
export function AsignaturaForm({ asignatura, onSaved, onCancel }: AsignaturaFormProps) {
  const [nombre, setNombre] = useState(asignatura?.nombre ?? "");
  const [error, setError] = useState<string | null>(null);

  const guardar = useApiMutation(
    asignatura
      ? (values: AsignaturaFormValues) => actualizarAsignatura(asignatura.id, values)
      : crearAsignatura,
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!nombre.trim()) {
      setError("El nombre es obligatorio.");
      return;
    }
    setError(null);
    const ok = await guardar.submit({ nombre: nombre.trim() });
    if (ok) onSaved();
  }

  return (
    <form className="module-form" onSubmit={handleSubmit} noValidate>
      <label>
        Nombre
        <input
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          aria-invalid={error ? true : undefined}
        />
      </label>
      {error && <p className="field-error">{error}</p>}

      {guardar.status === "error" && <ErrorMessage error={guardar.error} />}

      <div className="form-actions">
        <button type="button" className="btn" onClick={onCancel}>
          Cancelar
        </button>
        <button type="submit" className="btn btn-primary" disabled={guardar.status === "pending"}>
          {guardar.status === "pending" ? "Guardando…" : "Guardar asignatura"}
        </button>
      </div>
    </form>
  );
}
