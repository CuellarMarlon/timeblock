/**
 * Tipos comunes del contrato de la API (docs/api/contrato-api.md).
 */

/** Error en formato Problem Details (`application/problem+json`). */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  /** Errores de validación por campo, presentes en algunos `400`. */
  errors?: Record<string, string[]>;
}

/** Respuesta paginada de los listados. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
}

/** Parámetros de paginación de los listados (`page` desde 1, `pageSize` hasta 100). */
export interface PageParams {
  page?: number;
  pageSize?: number;
}

/** Valores admitidos en la cadena de consulta. Los `undefined` y `null` se omiten. */
export type QueryParams = Record<string, string | number | boolean | null | undefined>;
