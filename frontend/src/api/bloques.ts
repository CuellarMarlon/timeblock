import { apiClient } from "./client";
import type { PageParams, PagedResult } from "./types";

export type TipoActividad = "teoria" | "practica" | "laboratorio";
export type NivelComplejidad = "bajo" | "medio" | "alto";

// Opciones de los selectores, con el texto que ve el usuario
export const TIPOS_ACTIVIDAD: { value: TipoActividad; label: string }[] = [
  { value: "teoria", label: "Teoría" },
  { value: "practica", label: "Práctica" },
  { value: "laboratorio", label: "Laboratorio" },
];

export const NIVELES_COMPLEJIDAD: { value: NivelComplejidad; label: string }[] = [
  { value: "bajo", label: "Bajo" },
  { value: "medio", label: "Medio" },
  { value: "alto", label: "Alto" },
];

export function etiquetaTipoActividad(tipo: TipoActividad): string {
  return TIPOS_ACTIVIDAD.find((t) => t.value === tipo)?.label ?? tipo;
}

export function etiquetaNivelComplejidad(nivel: NivelComplejidad): string {
  return NIVELES_COMPLEJIDAD.find((n) => n.value === nivel)?.label ?? nivel;
}

export interface Bloque {
  id: string;
  descripcion: string;
  tipoActividad: TipoActividad;
  nivelComplejidad: NivelComplejidad;
  asignaturaId: string;
  coordinadorId: string;
  programasIds: string[];
  activo?: boolean;
}

export function cambiarEstadoBloque(id: string, activo: boolean) {
  return apiClient.put<Bloque>(`/api/bloques/${id}/estado`, { activo });
}

export interface CrearBloque {
  descripcion: string;
  tipoActividad: TipoActividad;
  nivelComplejidad: NivelComplejidad;
  asignaturaId: string;
  coordinadorId: string;
  programasIds: string[];
}

// Todos los campos son opcionales, pero la API pide mandar al menos uno
export interface ActualizarBloque {
  descripcion?: string;
  tipoActividad?: TipoActividad;
  nivelComplejidad?: NivelComplejidad;
  asignaturaId?: string;
}

export interface FiltrosBloques {
  programaId?: string;
  asignaturaId?: string;
  coordinadorId?: string;
  tipoActividad?: TipoActividad;
  nivelComplejidad?: NivelComplejidad;
}

export function listarBloques(
  filtros: FiltrosBloques,
  paginacion: PageParams,
  signal?: AbortSignal,
): Promise<PagedResult<Bloque>> {
  return apiClient.get<PagedResult<Bloque>>("/api/bloques", {
    query: { ...filtros, ...paginacion },
    signal,
  });
}

export function obtenerBloque(id: string, signal?: AbortSignal): Promise<Bloque> {
  return apiClient.get<Bloque>(`/api/bloques/${id}`, { signal });
}

export function crearBloque(datos: CrearBloque): Promise<Bloque> {
  return apiClient.post<Bloque>("/api/bloques", datos);
}

export function actualizarBloque(id: string, cambios: ActualizarBloque): Promise<Bloque> {
  return apiClient.put<Bloque>(`/api/bloques/${id}`, cambios);
}

// Asocia el bloque a un programa. Si ya estaba asociado no da error
export function asociarBloqueAPrograma(bloqueId: string, programaId: string): Promise<void> {
  return apiClient.put<void>(`/api/bloques/${bloqueId}/programas/${programaId}`);
}

export function desasociarBloqueDePrograma(bloqueId: string, programaId: string): Promise<void> {
  return apiClient.delete<void>(`/api/bloques/${bloqueId}/programas/${programaId}`);
}

export function eliminarBloque(bloqueId: string): Promise<void> {
  return apiClient.delete<void>(`/api/bloques/${bloqueId}`);
}
