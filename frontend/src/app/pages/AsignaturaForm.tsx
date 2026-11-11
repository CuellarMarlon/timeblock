import { useState, type FormEvent } from "react";
import { crearAsignatura } from "../../api/program";
import { useApiMutation } from "../../api/useApiMutation";
import { ErrorMessage } from "../components/ErrorMessage";

interface AsignaturaFormProps {
  onCreated: () => void;
  onCancel: () => void;
}

/** Formulario de creación de asignaturas con validación y errores de la API. */
export function AsignaturaForm({ onCreated, onCancel }: AsignaturaFormProps) {
  const [nombre, setNombre] = useState("");
  const [error, setError] = useState<string | null>(null);
  const crear = useApiMutation(crearAsignatura);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!nombre.trim()) {
      setError("El nombre es obligatorio.");
      return;
    }
    setError(null);
    const ok = await crear.submit({ nombre: nombre.trim() });
    if (ok) onCreated();
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

      {crear.status === "error" && <ErrorMessage error={crear.error} />}

      <div className="form-actions">
        <button type="button" className="btn" onClick={onCancel}>
          Cancelar
        </button>
        <button type="submit" className="btn btn-primary" disabled={crear.status === "pending"}>
          {crear.status === "pending" ? "Guardando…" : "Guardar asignatura"}
        </button>
      </div>
    </form>
  );
}
