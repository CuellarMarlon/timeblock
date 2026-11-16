import { apiClient } from "./client";
import type { PagedResult } from "./types";

/** Tamaño máximo de página que admite la API (docs/api/contrato-api.md). */
export const MAX_PAGE_SIZE = 100;

export interface Programa {
  id: string;
  nombre: string;
  planEstudio: string;
}

export interface Asignatura {
  id: string;
  nombre: string;
}

/** Persona con perfil de coordinador, usada para atribuir bloques y horarios. */
export interface Coordinador {
  id: string;
  nombre: string;
}

/** Datos de referencia que alimentan los selectores y los nombres mostrados. */
export interface Catalogos {
  programas: Programa[];
  asignaturas: Asignatura[];
  coordinadores: Coordinador[];
}

export const CATALOGOS_VACIOS: Catalogos = {
  programas: [],
  asignaturas: [],
  coordinadores: [],
};

/**
 * Carga programas, asignaturas y coordinadores en paralelo.
 * Se piden hasta {@link MAX_PAGE_SIZE} elementos de cada uno.
 */
export async function cargarCatalogos(signal?: AbortSignal): Promise<Catalogos> {
  const query = { page: 1, pageSize: MAX_PAGE_SIZE };

  const [programas, asignaturas, coordinadores] = await Promise.all([
    apiClient.get<PagedResult<Programa>>("/api/programas", { query, signal }),
    apiClient.get<PagedResult<Asignatura>>("/api/asignaturas", { query, signal }),
    apiClient.get<PagedResult<Coordinador>>("/api/personas", {
      query: { ...query, tipoPerfil: "coordinador" },
      signal,
    }),
  ]);

  return {
    programas: programas.items,
    asignaturas: asignaturas.items,
    // Del perfil solo interesan el identificador y el nombre.
    coordinadores: coordinadores.items.map(({ id, nombre }) => ({ id, nombre })),
  };
}

/** Devuelve el nombre del elemento con ese id, o el propio id si no se encuentra. */
export function nombreDe(lista: { id: string; nombre: string }[], id: string): string {
  return lista.find((elemento) => elemento.id === id)?.nombre ?? id;
}
