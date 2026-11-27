import { apiClient } from "./client";
import type { PagedResult, PageParams } from "./types";

/** Perfil de estudiante */
export interface PerfilEstudiante {
  tipo: "estudiante";
  edad: number;
  grupo: string;
  coordinadorId: string;
}

/** Perfil de coordinador*/
export interface PerfilCoordinador {
  tipo: "coordinador";
  especialidad: string;
}

/** Perfil de jefe de departamento */
export interface PerfilJefeDepartamento {
  tipo: "jefeDepartamento";
}

/** distingue la variante. */
export type Perfil = PerfilEstudiante | PerfilCoordinador | PerfilJefeDepartamento;

/** Tipos de perfil admitidos por el contrato. */
export type TipoPerfil = Perfil["tipo"];

/** Persona (PersonaResponse del contrato). */
export interface Persona {
  id: string;
  nombre: string;
  perfil: Perfil;
  activo?: boolean;
}

export function cambiarEstadoPersona(id: string, activo: boolean) {
  return apiClient.put<Persona>(`/api/personas/${id}/estado`, { activo });
}

/** Datos para crear una persona con su perfil. */
export interface PersonaInput {
  nombre: string;
  perfil: Perfil;
}

/** Autorización de un coordinador sobre un programa (AutorizacionResponse). */
export interface Autorizacion {
  coordinadorId: string;
  programaId: string;
  fechaInicio: string;
  fechaFin: string | null;
  puedeGenerar: boolean;
  puedeValidar: boolean;
}

/** Datos para crear una autorización. */
export interface AutorizacionInput {
  programaId: string;
  fechaInicio: string;
  fechaFin?: string | null;
  puedeGenerar: boolean;
  puedeValidar: boolean;
}

/** Datos para actualizar una autorización existente. */
export interface AutorizacionUpdate {
  fechaFin?: string | null;
  puedeGenerar: boolean;
  puedeValidar: boolean;
}

/** Filtros opcionales del listado de personas. */
export interface PersonaFiltros {
  /** Parte del nombre, sin distinguir mayúsculas. */
  nombre?: string;
  tipoPerfil?: TipoPerfil;
}

/** Lista personas con paginación y filtros opcionales. */
export function listarPersonas(params: PageParams & PersonaFiltros, signal?: AbortSignal) {
  return apiClient.get<PagedResult<Persona>>("/api/personas", { query: { ...params }, signal });
}

/** Consulta una persona por id. */
export function obtenerPersona(id: string, signal?: AbortSignal) {
  return apiClient.get<Persona>(`/api/personas/${id}`, { signal });
}

/** Elimina una persona con su perfil si no tiene dependencias. */
export function eliminarPersona(id: string) {
  return apiClient.delete<void>(`/api/personas/${id}`);
}

/** Crea una persona con su perfil. */
export function crearPersona(input: PersonaInput) {
  return apiClient.post<Persona>("/api/personas", input);
}

/** Actualiza el nombre de una persona. */
export function actualizarPersona(id: string, input: { nombre: string }) {
  return apiClient.put<Persona>(`/api/personas/${id}`, input);
}

/** Actualiza el perfil de una persona sin cambiar su tipo. */
export function actualizarPerfil(id: string, perfil: Perfil) {
  return apiClient.put<Persona>(`/api/personas/${id}/perfil`, perfil);
}

/** Lista las autorizaciones de un coordinador (lista simple, sin paginar). */
export function listarAutorizaciones(coordinadorId: string, signal?: AbortSignal) {
  return apiClient.get<Autorizacion[]>(`/api/coordinadores/${coordinadorId}/autorizaciones`, {
    signal,
  });
}

/** Crea una autorización para un coordinador. */
export function crearAutorizacion(coordinadorId: string, input: AutorizacionInput) {
  return apiClient.post<Autorizacion>(`/api/coordinadores/${coordinadorId}/autorizaciones`, input);
}

/** Actualiza una autorización identificada por programa y fecha de inicio. */
export function actualizarAutorizacion(
  coordinadorId: string,
  programaId: string,
  fechaInicio: string,
  input: AutorizacionUpdate,
) {
  return apiClient.put<Autorizacion>(
    `/api/coordinadores/${coordinadorId}/autorizaciones/${programaId}/${fechaInicio}`,
    input,
  );
}

/** Elimina una autorización identificada por coordinador, programa y fecha de inicio. */
export function eliminarAutorizacion(
  coordinadorId: string,
  programaId: string,
  fechaInicio: string,
) {
  return apiClient.delete<void>(
    `/api/coordinadores/${coordinadorId}/autorizaciones/${programaId}/${fechaInicio}`,
  );
}
