import { apiClient } from "./client";
import type { PagedResult } from "./types";

// Máximo de elementos por página que acepta la API
export const MAX_PAGE_SIZE = 100;

export interface Programa {
  id: string;
  nombre: string;
  planEstudio: string;
  activo?: boolean;
}

export interface Asignatura {
  id: string;
  nombre: string;
  activo?: boolean;
}

// Persona con perfil de coordinador. Guardamos solo lo que usan los selectores
export interface Coordinador {
  id: string;
  nombre: string;
  activo?: boolean;
}

// Datos de apoyo para llenar los selectores y mostrar nombres en vez de ids
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

// Pide programas, asignaturas y coordinadores a la vez (hasta 100 de cada uno)
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
    // De cada persona solo hacen falta el id y el nombre
    coordinadores: coordinadores.items.map(({ id, nombre, activo }) => ({ id, nombre, activo })),
  };
}

// Busca el nombre por id. Si no lo encuentra, devuelve el id tal cual
export function nombreDe(
  lista: { id: string; nombre: string; activo?: boolean }[],
  id: string,
): string {
  const encontrado = lista.find((elemento) => elemento.id === id);
  return encontrado
    ? `${encontrado.nombre}${encontrado.activo === false ? " (inactivo)" : ""}`
    : id;
}
