import { useCallback, useMemo, useState, type FormEvent } from "react";
import {
  listarMatriculasDeEstudiante,
  listarMatriculasDePrograma,
  matricular,
} from "../../api/enrollments";
import { listarPersonas } from "../../api/persons";
import { listarProgramas } from "../../api/program";
import { useApiMutation } from "../../api/useApiMutation";
import { useApiQuery } from "../../api/useApiQuery";
import { ErrorMessage } from "../components/ErrorMessage";
import { QueryContent } from "../components/QueryContent";
import { Tabs } from "../components/Tabs";
import "./ProgramasPage.css";

const MAX_PAGE_SIZE = 100;

const TABS = [
  { id: "programa", label: "Por programa" },
  { id: "estudiante", label: "Por estudiante" },
];

interface MatriculaInput {
  programaId: string;
  estudianteId: string;
}

/** Acción de matrícula definida fuera del componente para que sea estable. */
function crearMatricula({ programaId, estudianteId }: MatriculaInput) {
  return matricular(programaId, estudianteId);
}

/** Módulo de matrícula: alta de matrículas y consulta por programa o por estudiante. */
export function MatriculaPage() {
  const [programaId, setProgramaId] = useState("");
  const [estudianteId, setEstudianteId] = useState("");
  const [errors, setErrors] = useState<{ programa?: string; estudiante?: string }>({});
  const [notice, setNotice] = useState<string | null>(null);
  const [tab, setTab] = useState("programa");
  const [refreshKey, setRefreshKey] = useState(0);
  const mutation = useApiMutation(crearMatricula);

  const loadProgramas = useCallback(
    (signal: AbortSignal) => listarProgramas({ page: 1, pageSize: MAX_PAGE_SIZE }, signal),
    [],
  );
  const loadPersonas = useCallback(
    (signal: AbortSignal) => listarPersonas({ page: 1, pageSize: MAX_PAGE_SIZE }, signal),
    [],
  );
  const programas = useApiQuery(loadProgramas);
  const personas = useApiQuery(loadPersonas);

  const estudiantes = useMemo(
    () =>
      personas.status === "success"
        ? personas.data.items.filter((p) => p.perfil.tipo === "estudiante")
        : [],
    [personas],
  );
  const nombresProgramas = useMemo(
    () =>
      programas.status === "success"
        ? new Map(programas.data.items.map((p) => [p.id, p.nombre]))
        : new Map<string, string>(),
    [programas],
  );
  const nombresPersonas = useMemo(
    () =>
      personas.status === "success"
        ? new Map(personas.data.items.map((p) => [p.id, p.nombre]))
        : new Map<string, string>(),
    [personas],
  );

  // Consulta de la pestaña activa. Sin selección no se pide nada.
  const consultaId = tab === "programa" ? programaId : estudianteId;
  const loadMatriculas = useCallback(
    (signal: AbortSignal) => {
      void refreshKey;
      if (!consultaId) return Promise.resolve(null);
      return tab === "programa"
        ? listarMatriculasDePrograma(consultaId, { page: 1, pageSize: MAX_PAGE_SIZE }, signal)
        : listarMatriculasDeEstudiante(consultaId, { page: 1, pageSize: MAX_PAGE_SIZE }, signal);
    },
    [tab, consultaId, refreshKey],
  );
  const matriculas = useApiQuery(loadMatriculas);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setNotice(null);

    const nuevos: typeof errors = {};
    if (!programaId) nuevos.programa = "Selecciona un programa.";
    if (!estudianteId) nuevos.estudiante = "Selecciona un estudiante.";
    setErrors(nuevos);
    if (Object.keys(nuevos).length > 0) return;

    const ok = await mutation.submit({ programaId, estudianteId });
    if (ok) {
      setNotice("Estudiante matriculado correctamente.");
      setRefreshKey((k) => k + 1);
    }
  }

  return (
    <section className="module" aria-labelledby="matricula-titulo">
      <header>
        <h1 id="matricula-titulo">Matrícula</h1>
        <p>Registro y consulta de la matrícula de estudiantes en programas académicos.</p>
      </header>

      <QueryContent
        query={programas}
        onRetry={programas.reload}
        loadingLabel="Cargando programas…"
        emptyMessage="Crea al menos un programa para poder matricular."
      >
        {(dataProgramas) => (
          <QueryContent
            query={personas}
            onRetry={personas.reload}
            loadingLabel="Cargando estudiantes…"
            emptyMessage="Crea al menos un estudiante para poder matricular."
          >
            {() => (
              <form className="card associate-form" onSubmit={handleSubmit} noValidate>
                <label>
                  Estudiante
                  <select
                    value={estudianteId}
                    onChange={(e) => {
                      setEstudianteId(e.target.value);
                      setNotice(null);
                      mutation.reset();
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
                  {errors.estudiante && <span className="field-error">{errors.estudiante}</span>}
                </label>

                <label>
                  Programa académico
                  <select
                    value={programaId}
                    onChange={(e) => {
                      setProgramaId(e.target.value);
                      setNotice(null);
                      mutation.reset();
                    }}
                    aria-invalid={errors.programa ? true : undefined}
                  >
                    <option value="">Selecciona un programa</option>
                    {dataProgramas.items.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.nombre}
                      </option>
                    ))}
                  </select>
                  {errors.programa && <span className="field-error">{errors.programa}</span>}
                </label>

                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={mutation.status === "pending"}
                >
                  {mutation.status === "pending" ? "Matriculando…" : "Matricular"}
                </button>
              </form>
            )}
          </QueryContent>
        )}
      </QueryContent>

      {notice && (
        <p className="notice" role="status">
          {notice}
        </p>
      )}
      {mutation.status === "error" && <ErrorMessage error={mutation.error} />}

      <h2>Consultar matrículas</h2>
      <Tabs label="Tipo de consulta" tabs={TABS} active={tab} onChange={setTab} />

      {!consultaId ? (
        <p className="notice">
          {tab === "programa"
            ? "Selecciona un programa arriba para ver sus estudiantes."
            : "Selecciona un estudiante arriba para ver sus programas."}
        </p>
      ) : (
        <QueryContent
          query={matriculas}
          onRetry={matriculas.reload}
          loadingLabel="Cargando matrículas…"
          emptyMessage={
            tab === "programa"
              ? "Este programa todavía no tiene estudiantes matriculados."
              : "Este estudiante todavía no está matriculado en ningún programa."
          }
        >
          {(data) =>
            data && (
              <div className="card">
                <table className="table">
                  <thead>
                    <tr>
                      <th scope="col">Estudiante</th>
                      <th scope="col">Programa académico</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.items.map((m) => (
                      <tr key={`${m.estudianteId}-${m.programaId}`}>
                        <td>{nombresPersonas.get(m.estudianteId) ?? "Estudiante desconocido"}</td>
                        <td>{nombresProgramas.get(m.programaId) ?? "Programa desconocido"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )
          }
        </QueryContent>
      )}
    </section>
  );
}
