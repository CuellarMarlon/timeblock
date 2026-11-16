import { useId } from "react";
import { Link } from "react-router-dom";
import { cargarCatalogos } from "../../api/catalogos";
import { useApiQuery } from "../../api/useApiQuery";
import { QueryContent } from "../components/QueryContent";
import { HorarioForm } from "./HorarioForm";

/** Página para crear un horario de armado manual. */
export function HorarioNuevoPage() {
  const headingId = useId();
  const catalogos = useApiQuery(cargarCatalogos);

  return (
    <section aria-labelledby={headingId}>
      <h1 id={headingId}>Nuevo horario</h1>
      <p>
        <Link to="/horarios">← Volver a los horarios</Link>
      </p>
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
