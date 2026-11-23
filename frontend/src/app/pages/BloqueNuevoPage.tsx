import { useId } from "react";
import { cargarCatalogos } from "../../api/catalogos";
import { useApiQuery } from "../../api/useApiQuery";
import { QueryContent } from "../components/QueryContent";
import { BloqueForm } from "./BloqueForm";
import "./BloquesHorarios.css";
import "./ProgramasPage.css";

// Página para crear un bloque de contenido
export function BloqueNuevoPage() {
  const headingId = useId();
  const catalogos = useApiQuery(cargarCatalogos);

  return (
    <section className="module" aria-labelledby={headingId}>
      <header className="page-header">
        <div>
          <h1 id={headingId}>Nuevo bloque</h1>
          <p>Registra un bloque de contenido y elige en qué programas se puede utilizar.</p>
        </div>
      </header>

      <QueryContent
        query={catalogos}
        onRetry={catalogos.reload}
        loadingLabel="Cargando datos del formulario…"
        isEmpty={() => false}
      >
        {(datos) => <BloqueForm catalogos={datos} />}
      </QueryContent>
    </section>
  );
}
