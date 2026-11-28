import { apiClient } from "./client";
import type { PageParams, PagedResult } from "./types";

export const EDIFICIO_MAX_LENGTH = 100;
export const CAPACIDAD_MAX = 1000;

export interface Aula {
  id: string;
  edificio: string;
  capacidad: number;
}

export interface DatosAula {
  edificio: string;
  capacidad: number;
}

export interface FiltrosAulas {
  edificio?: string;
}

export function listarAulas(
  filtros: FiltrosAulas,
  paginacion: PageParams,
  signal?: AbortSignal,
): Promise<PagedResult<Aula>> {
  return apiClient.get<PagedResult<Aula>>("/api/aulas", {
    query: { ...filtros, ...paginacion },
    signal,
  });
}

export function obtenerAula(id: string, signal?: AbortSignal): Promise<Aula> {
  return apiClient.get<Aula>(`/api/aulas/${id}`, { signal });
}

export function crearAula(datos: DatosAula): Promise<Aula> {
  return apiClient.post<Aula>("/api/aulas", datos);
}

export function actualizarAula(id: string, datos: DatosAula): Promise<Aula> {
  return apiClient.put<Aula>(`/api/aulas/${id}`, datos);
}
