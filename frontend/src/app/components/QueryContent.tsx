import type { ReactNode } from "react";
import type { QueryState } from "../../api/useApiQuery";
import { EmptyState } from "./EmptyState";
import { ErrorMessage } from "./ErrorMessage";
import { LoadingIndicator } from "./LoadingIndicator";

interface QueryContentProps<T> {
  query: QueryState<T>;
  children: (data: T) => ReactNode;
  onRetry?: () => void;
  loadingLabel?: string;
  emptyMessage?: string;
  /**
   * Decide si el resultado está vacío. Por defecto se consideran vacíos
   * `undefined`, `null`, los arreglos vacíos y las páginas sin `items`.
   */
  isEmpty?: (data: T) => boolean;
}

/** Muestra la carga, el error, el estado vacío o el contenido de una consulta. */
export function QueryContent<T>({
  query,
  children,
  onRetry,
  loadingLabel,
  emptyMessage,
  isEmpty = isEmptyResult,
}: QueryContentProps<T>) {
  switch (query.status) {
    case "loading":
      return <LoadingIndicator label={loadingLabel} />;
    case "error":
      return <ErrorMessage error={query.error} onRetry={onRetry} />;
    case "success":
      return isEmpty(query.data) ? <EmptyState message={emptyMessage} /> : children(query.data);
  }
}

function isEmptyResult(data: unknown): boolean {
  if (data === undefined || data === null) return true;
  if (Array.isArray(data)) return data.length === 0;
  if (typeof data === "object" && "items" in data && Array.isArray(data.items)) {
    return data.items.length === 0;
  }
  return false;
}
