import { useCallback, useId } from "react";
import { useParams } from "react-router-dom";
import { obtenerBloque } from "../../api/bloques";
import { cargarCatalogos } from "../../api/catalogos";
import { useApiQuery } from "../../api/useApiQuery";
import { QueryContent } from "../components/QueryContent";
import { BloqueForm } from "./BloqueForm";

/** Página para editar un bloque existente. */
export function BloqueEditarPage() {
  const headingId = useId();
  const { bloqueId = "" } = useParams();

  const catalogos = useApiQuery(cargarCatalogos);
  const load = useCallback((signal: AbortSignal) => obtenerBloque(bloqueId, signal), [bloqueId]);
  const bloque = useApiQuery(load);

  return (
    <section aria-labelledby={headingId}>
      <h1 id={headingId}>Editar bloque</h1>
      <QueryContent
        query={bloque}
        onRetry={bloque.reload}
        loadingLabel="Cargando bloque…"
        isEmpty={() => false}
      >
        {(datosBloque) => (
          <QueryContent
            query={catalogos}
            onRetry={catalogos.reload}
            loadingLabel="Cargando datos del formulario…"
            isEmpty={() => false}
          >
            {(datosCatalogos) => (
              <BloqueForm key={datosBloque.id} catalogos={datosCatalogos} bloque={datosBloque} />
            )}
          </QueryContent>
        )}
      </QueryContent>
    </section>
  );
}
