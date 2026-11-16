import { useId } from "react";
import { cargarCatalogos } from "../../api/catalogos";
import { useApiQuery } from "../../api/useApiQuery";
import { QueryContent } from "../components/QueryContent";
import { BloqueForm } from "./BloqueForm";

/** Página para crear un bloque de contenido. */
export function BloqueNuevoPage() {
  const headingId = useId();
  const catalogos = useApiQuery(cargarCatalogos);

  return (
    <section aria-labelledby={headingId}>
      <h1 id={headingId}>Nuevo bloque</h1>
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
