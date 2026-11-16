import { apiClient } from "./client";
import type { NivelComplejidad, TipoActividad } from "./bloques";
import type { PageParams, PagedResult } from "./types";

/** La API serializa el estado en camelCase; en M4 solo existe `generado`. */
export type EstadoHorario = "generado";

export function etiquetaEstadoHorario(estado: EstadoHorario): string {
  return estado.toUpperCase();
}

/** Bloque asociado a un horario, con la fecha en que se imparte (`YYYY-MM-DD`). */
export interface BloqueEnHorario {
  bloqueId: string;
  descripcion: string;
  asignaturaId: string;
  tipoActividad: TipoActividad;
  nivelComplejidad: NivelComplejidad;
  fecha: string;
  /** En M4 los cuatro campos siguientes llegan siempre nulos. */
  horaInicio: string | null;
  horaFin: string | null;
  periodoImp: string | null;
  aulaId: string | null;
}

export interface Horario {
  id: string;
  programaId: string;
  periodoAcademico: string;
  coordinadorId: string;
  fechaInicioAutorizacion: string;
  fechaCreacion: string;
  estado: EstadoHorario;
  cantTotalBloques: number;
  bloques: BloqueEnHorario[];
}

/**
 * Cuerpo de la creación. El cliente no envía el estado, la fecha de creación
 * ni la fecha de inicio de la autorización: las determina el servidor.
 */
export interface CrearHorario {
  programaId: string;
  periodoAcademico: string;
  coordinadorId: string;
  bloques: { bloqueId: string; fecha: string }[];
}

export interface FiltrosHorarios {
  programaId?: string;
  periodoAcademico?: string;
}

export function listarHorarios(
  filtros: FiltrosHorarios,
  paginacion: PageParams,
  signal?: AbortSignal,
): Promise<PagedResult<Horario>> {
  return apiClient.get<PagedResult<Horario>>("/api/horarios", {
    query: { ...filtros, ...paginacion },
    signal,
  });
}

export function obtenerHorario(id: string, signal?: AbortSignal): Promise<Horario> {
  return apiClient.get<Horario>(`/api/horarios/${id}`, { signal });
}

export function crearHorario(datos: CrearHorario): Promise<Horario> {
  return apiClient.post<Horario>("/api/horarios", datos);
}
