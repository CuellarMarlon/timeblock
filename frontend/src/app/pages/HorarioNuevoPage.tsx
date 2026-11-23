import { useId } from "react";
import { Link } from "react-router-dom";
import { cargarCatalogos } from "../../api/catalogos";
import { useApiQuery } from "../../api/useApiQuery";
import { QueryContent } from "../components/QueryContent";
import { HorarioForm } from "./HorarioForm";
import "./BloquesHorarios.css";
import "./ProgramasPage.css";

// Página para armar un horario a mano
export function HorarioNuevoPage() {
  const headingId = useId();
  const catalogos = useApiQuery(cargarCatalogos);

  return (
    <section className="module" aria-labelledby={headingId}>
      <header className="page-header">
        <div>
          <h1 id={headingId}>Nuevo horario</h1>
          <p>Elige el programa, el periodo académico y la fecha de cada bloque.</p>
        </div>
        <Link className="btn" to="/horarios">
          ← Volver a los horarios
        </Link>
      </header>

      <QueryContent
        query={catalogos}
        onRetry={catalogos.reload}
        loadingLabel="Cargando datos del formulario…"
        isEmpty={() => false}
      >
        {(datos) => <HorarioForm catalogos={datos} />}
      </QueryContent>
    </section>
  );
}
