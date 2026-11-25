import { useState, type FormEvent } from "react";
import { listarMatriculas, matricular } from "../../api/enrollments";
import { useApiMutation } from "../../api/useApiMutation";
import { BuscadorSelect } from "../components/BuscadorSelect";
import { ErrorMessage } from "../components/ErrorMessage";
import { buscarEstudiantes, buscarProgramas } from "./matriculaBusquedas";

interface MatriculaFormProps {
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
export function MatriculaForm({ onSaved, onCancel }: MatriculaFormProps) {
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

    const ok = await mutation.run({ programaId, estudianteId });
    if (ok.ok) onSaved();
  }

  return (
    <form className="module-form" onSubmit={handleSubmit} noValidate>
      <BuscadorSelect
        label="Estudiante"
        searchLabel="Buscar estudiante"
        placeholder="Selecciona un estudiante"
        value={estudianteId}
        buscar={buscarEstudiantes}
        error={errors.estudiante}
        onChange={(id) => {
          setEstudianteId(id);
          setDuplicada(false);
        }}
      />

      <BuscadorSelect
        label="Programa académico"
        searchLabel="Buscar programa"
        placeholder="Selecciona un programa"
        value={programaId}
        buscar={buscarProgramas}
        error={errors.programa}
        onChange={(id) => {
          setProgramaId(id);
          setDuplicada(false);
        }}
      />

      {duplicada && (
        <p className="notice" role="alert">
          Este estudiante ya está matriculado en ese programa.
        </p>
      )}
      {mutation.state.status === "error" && <ErrorMessage error={mutation.state.error} />}

      <div className="form-actions">
        <button type="button" className="btn" onClick={onCancel}>
          Cancelar
        </button>
        <button
          type="submit"
          className="btn btn-primary"
          disabled={comprobando || mutation.state.status === "loading"}
        >
          {comprobando || mutation.state.status === "loading" ? "Matriculando…" : "Matricular"}
        </button>
      </div>
    </form>
  );
}
