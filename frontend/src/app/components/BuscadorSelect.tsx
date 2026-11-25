import { useCallback, useEffect, useId, useState } from "react";
import type { PagedResult } from "../../api/types";
import { useApiQuery } from "../../api/useApiQuery";
import { ErrorMessage } from "./ErrorMessage";
import "./BuscadorSelect.css";

/** Registro que se puede elegir en el selector: basta con su id y su nombre. */
export interface OpcionBuscable {
  id: string;
  nombre: string;
}

/** Consulta a la API los registros cuyo nombre contiene el texto indicado. */
export type BuscarOpciones = (
  texto: string,
  pageSize: number,
  signal: AbortSignal,
) => Promise<PagedResult<OpcionBuscable>>;

/** Espera tras la última tecla antes de consultar la API. */
const RETARDO_BUSQUEDA_MS = 300;
/** Resultados que se piden en cada búsqueda. */
const RESULTADOS_POR_BUSQUEDA = 20;

interface BuscadorSelectProps {
  /** Etiqueta del selector ("Estudiante" o "Filtrar por estudiante"). */
  label: string;
  /** Etiqueta del campo de búsqueda ("Buscar estudiante"). */
  searchLabel: string;
  /** Texto de la opción vacía. */
  placeholder: string;
  value: string;
  onChange: (id: string) => void;
  /** Debe ser estable: cada vez que cambia se repite la búsqueda. */
  buscar: BuscarOpciones;
  /** Mensaje de validación que se muestra debajo del campo. */
  error?: string;
  /** `filter` para las barras de filtros: sin etiqueta visible. */
  variant?: "field" | "filter";
}

/**
 * Selector que consulta la API mientras se escribe, en lugar de cargar una sola
 * página de registros. Así se puede elegir cualquier registro aunque haya más
 * de los que caben en una página.
 */
export function BuscadorSelect({
  label,
  searchLabel,
  placeholder,
  value,
  onChange,
  buscar,
  error,
  variant = "field",
}: BuscadorSelectProps) {
  const errorId = `${useId()}-error`;
  const [texto, setTexto] = useState("");
  const [consulta, setConsulta] = useState("");
  // Se recuerda la opción elegida para que siga visible si una búsqueda posterior no la incluye.
  const [elegida, setElegida] = useState<OpcionBuscable | null>(null);

  useEffect(() => {
    const temporizador = setTimeout(() => setConsulta(texto.trim()), RETARDO_BUSQUEDA_MS);
    return () => clearTimeout(temporizador);
  }, [texto]);

  const cargar = useCallback(
    (signal: AbortSignal) => buscar(consulta, RESULTADOS_POR_BUSQUEDA, signal),
    [buscar, consulta],
  );
  const resultados = useApiQuery(cargar);

  const encontradas = resultados.status === "success" ? resultados.data.items : [];
  const opciones =
    elegida && elegida.id === value && !encontradas.some((o) => o.id === elegida.id)
      ? [elegida, ...encontradas]
      : encontradas;

  function elegir(id: string) {
    setElegida(opciones.find((o) => o.id === id) ?? null);
    onChange(id);
  }

  const select = (
    <select
      aria-label={variant === "filter" ? label : undefined}
      value={value}
      aria-invalid={error ? true : undefined}
      aria-describedby={error ? errorId : undefined}
      onChange={(event) => elegir(event.target.value)}
    >
      <option value="">{placeholder}</option>
      {opciones.map((o) => (
        <option key={o.id} value={o.id}>
          {o.nombre}
        </option>
      ))}
    </select>
  );

  return (
    <div className={variant === "filter" ? "buscador buscador--filtro" : "field buscador"}>
      <input
        type="search"
        aria-label={searchLabel}
        placeholder={`${searchLabel}…`}
        value={texto}
        onChange={(event) => setTexto(event.target.value)}
      />
      {variant === "filter" ? (
        select
      ) : (
        <label>
          {label}
          {select}
        </label>
      )}
      {resultados.status === "loading" && <p className="buscador__estado">Buscando…</p>}
      {resultados.status === "success" && (
        <EstadoResultados consulta={consulta} data={resultados.data} />
      )}
      {resultados.status === "error" && (
        <ErrorMessage error={resultados.error} onRetry={resultados.reload} />
      )}
      {error && (
        <p className="field-error" id={errorId}>
          {error}
        </p>
      )}
    </div>
  );
}

function EstadoResultados({
  consulta,
  data,
}: {
  consulta: string;
  data: PagedResult<OpcionBuscable>;
}) {
  if (data.totalItems === 0) {
    return (
      <p className="buscador__estado">
        {consulta ? `Sin resultados para «${consulta}».` : "No hay registros."}
      </p>
    );
  }
  if (data.totalItems > data.items.length) {
    return (
      <p className="buscador__estado">
        Mostrando {data.items.length} de {data.totalItems}. Escribe para afinar la búsqueda.
      </p>
    );
  }
  return null;
}
