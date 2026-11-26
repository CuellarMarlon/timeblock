import { apiClient } from "./client";
import type { PagedResult, PageParams } from "./types";

/** Matrícula de un estudiante en un programa (MatriculaResponse). */
export interface Matricula {
  estudianteId: string;
  programaId: string;
}

/** Matricula a un estudiante en un programa. */
export function matricular(programaId: string, estudianteId: string) {
  return apiClient.post<Matricula>(`/api/programas/${programaId}/matriculas`, { estudianteId });
}

/** Elimina la matrícula de un estudiante en un programa. */
export function desmatricular(programaId: string, estudianteId: string) {
  return apiClient.delete<void>(
    `/api/programas/${programaId}/matriculas/${estudianteId}`,
  );
}

/** Lista las matrículas de un programa (estudiantes matriculados). */
export function listarMatriculasDePrograma(
  programaId: string,
  params: PageParams,
  signal?: AbortSignal,
) {
  return apiClient.get<PagedResult<Matricula>>(`/api/programas/${programaId}/matriculas`, {
    query: { ...params },
    signal,
  });
}

/** Lista las matrículas de un estudiante (programas en los que está matriculado). */
export function listarMatriculasDeEstudiante(
  estudianteId: string,
  params: PageParams,
  signal?: AbortSignal,
) {
  return apiClient.get<PagedResult<Matricula>>(`/api/estudiantes/${estudianteId}/matriculas`, {
    query: { ...params },
    signal,
  });
}

/** Filtros opcionales del listado general de matrículas. */
export interface MatriculaFiltros {
  estudianteId?: string;
  programaId?: string;
}

/**
 * Lista matrículas con filtros opcionales.
 * Endpoint asumido, pendiente en el backend
 */
export function listarMatriculas(
  filtros: MatriculaFiltros,
  params: PageParams,
  signal?: AbortSignal,
) {
  return apiClient.get<PagedResult<Matricula>>("/api/matriculas", {
    query: { ...filtros, ...params },
    signal,
  });
}
