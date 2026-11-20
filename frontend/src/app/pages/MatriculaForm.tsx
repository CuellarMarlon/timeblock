import { useState, type FormEvent } from "react";
import { listarMatriculas, matricular } from "../../api/enrollments";
import type { Persona } from "../../api/persons";
import type { Programa } from "../../api/program";
import { useApiMutation } from "../../api/useApiMutation";
import { ErrorMessage } from "../components/ErrorMessage";

interface MatriculaFormProps {
  estudiantes: Persona[];
  programas: Programa[];
  onSaved: () => void;
  onCancel: () => void;
}

interface MatriculaInput {
  programaId: string;
  estudianteId: string;
}

/** Acción de matrícula definida fuera del componente para que sea estable. */
function crearMatricula({ programaId, estudianteId }: MatriculaInput) {
  return matricular(programaId, estudianteId);
}

/** Formulario de nueva matrícula: avisa si el estudiante ya está en ese programa. */
export function MatriculaForm({ estudiantes, programas, onSaved, onCancel }: MatriculaFormProps) {
  const [estudianteId, setEstudianteId] = useState("");
  const [programaId, setProgramaId] = useState("");
  const [errors, setErrors] = useState<{ estudiante?: string; programa?: string }>({});
  const [duplicada, setDuplicada] = useState(false);
  const [comprobando, setComprobando] = useState(false);
  const mutation = useApiMutation(crearMatricula);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setDuplicada(false);

    const nuevos: typeof errors = {};
    if (!estudianteId) nuevos.estudiante = "Selecciona un estudiante.";
    if (!programaId) nuevos.programa = "Selecciona un programa.";
    setErrors(nuevos);
    if (Object.keys(nuevos).length > 0) return;

    setComprobando(true);
    try {
      const existentes = await listarMatriculas(
        { estudianteId, programaId },
        { page: 1, pageSize: 1 },
      );
      if (existentes.totalItems > 0) {
        setDuplicada(true);
        return;
      }
    } catch {
      // Si la comprobación falla, la API decide al intentar matricular.
    } finally {
      setComprobando(false);
    }

    const ok = await mutation.submit({ programaId, estudianteId });
    if (ok) onSaved();
  }

  return (
    <form className="module-form" onSubmit={handleSubmit} noValidate>
      <label>
        Estudiante
        <select
          value={estudianteId}
          onChange={(e) => {
            setEstudianteId(e.target.value);
            setDuplicada(false);
          }}
          aria-invalid={errors.estudiante ? true : undefined}
        >
          <option value="">Selecciona un estudiante</option>
          {estudiantes.map((e) => (
            <option key={e.id} value={e.id}>
              {e.nombre}
            </option>
          ))}
        </select>
      </label>
      {errors.estudiante && <p className="field-error">{errors.estudiante}</p>}

      <label>
        Programa académico
        <select
          value={programaId}
          onChange={(e) => {
            setProgramaId(e.target.value);
            setDuplicada(false);
          }}
          aria-invalid={errors.programa ? true : undefined}
        >
          <option value="">Selecciona un programa</option>
          {programas.map((p) => (
            <option key={p.id} value={p.id}>
              {p.nombre}
            </option>
          ))}
        </select>
      </label>
      {errors.programa && <p className="field-error">{errors.programa}</p>}

      {duplicada && (
        <p className="notice" role="alert">
          Este estudiante ya está matriculado en ese programa.
        </p>
      )}
      {mutation.status === "error" && <ErrorMessage error={mutation.error} />}

      <div className="form-actions">
        <button type="button" className="btn" onClick={onCancel}>
          Cancelar
        </button>
        <button
          type="submit"
          className="btn btn-primary"
          disabled={comprobando || mutation.status === "pending"}
        >
          {comprobando || mutation.status === "pending" ? "Matriculando…" : "Matricular"}
        </button>
      </div>
    </form>
  );
}
