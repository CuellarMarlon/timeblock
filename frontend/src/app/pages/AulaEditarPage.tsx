import { useCallback, useId } from "react";
import { useParams } from "react-router-dom";
import { obtenerAula } from "../../api/aulas";
import { useApiQuery } from "../../api/useApiQuery";
import { QueryContent } from "../components/QueryContent";
import { AulaForm } from "./AulaForm";
import "./BloquesHorarios.css";
import "./ProgramasPage.css";

export function AulaEditarPage() {
  const headingId = useId();
  const { aulaId = "" } = useParams();

  const load = useCallback((signal: AbortSignal) => obtenerAula(aulaId, signal), [aulaId]);
  const aula = useApiQuery(load);

  return (
    <section className="module" aria-labelledby={headingId}>
      <header className="page-header">
        <div>
          <h1 id={headingId}>Editar aula</h1>
          <p>Cambia el edificio o la capacidad del aula.</p>
        </div>
      </header>

      <QueryContent
        query={aula}
        onRetry={aula.reload}
        loadingLabel="Cargando aula…"
        isEmpty={() => false}
      >
        {(datos) => <AulaForm key={datos.id} aula={datos} />}
      </QueryContent>
    </section>
  );
}
