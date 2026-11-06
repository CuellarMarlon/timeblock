import { apiClient } from "./client";
import type { PagedResult, PageParams } from "./types";

/** Programa académico (ProgramaResponse del contrato). */
export interface Programa {
  id: string;
  nombre: string;
  planEstudio: string;
}

/** Asignatura (AsignaturaResponse del contrato). */
export interface Asignatura {
  id: string;
  nombre: string;
}

/** Datos para crear o actualizar un programa. */
export interface ProgramaInput {
  nombre: string;
  planEstudio: string;
}

/** Datos para crear o actualizar una asignatura. */
export interface AsignaturaInput {
  nombre: string;
}

/** Lista programas con paginación. */
export function listarProgramas(params: PageParams, signal?: AbortSignal) {
  return apiClient.get<PagedResult<Programa>>("/api/programas", { query: { ...params }, signal });
}

/** Consulta un programa por id. */
export function obtenerPrograma(id: string, signal?: AbortSignal) {
  return apiClient.get<Programa>(`/api/programas/${id}`, { signal });
}

/** Crea un programa. */
export function crearPrograma(input: ProgramaInput) {
  return apiClient.post<Programa>("/api/programas", input);
}

/** Actualiza un programa. */
export function actualizarPrograma(id: string, input: ProgramaInput) {
  return apiClient.put<Programa>(`/api/programas/${id}`, input);
}

/** Lista asignaturas con paginación. */
export function listarAsignaturas(params: PageParams, signal?: AbortSignal) {
  return apiClient.get<PagedResult<Asignatura>>("/api/asignaturas", {
    query: { ...params },
    signal,
  });
}

/** Crea una asignatura. */
export function crearAsignatura(input: AsignaturaInput) {
  return apiClient.post<Asignatura>("/api/asignaturas", input);
}

/** Actualiza una asignatura. */
export function actualizarAsignatura(id: string, input: AsignaturaInput) {
  return apiClient.put<Asignatura>(`/api/asignaturas/${id}`, input);
}

/** Lista las asignaturas asociadas a un programa. */
export function listarAsignaturasDePrograma(
  programaId: string,
  params: PageParams,
  signal?: AbortSignal,
) {
  return apiClient.get<PagedResult<Asignatura>>(`/api/programas/${programaId}/asignaturas`, {
    query: { ...params },
    signal,
  });
}

/** Asocia una asignatura a un programa. */
export function asociarAsignatura(programaId: string, asignaturaId: string) {
  return apiClient.put<void>(`/api/programas/${programaId}/asignaturas/${asignaturaId}`);
}
