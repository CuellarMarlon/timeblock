import { listarPersonas } from "../../api/persons";
import { listarProgramas } from "../../api/program";

/*
 * Búsquedas de los selectores de matrícula. Se definen fuera de los componentes
 * para que sean estables y no repitan la consulta en cada render.
 */

/** Busca estudiantes por nombre. Sin texto, devuelve los primeros. */
export function buscarEstudiantes(texto: string, pageSize: number, signal: AbortSignal) {
  return listarPersonas(
    { nombre: texto || undefined, tipoPerfil: "estudiante", page: 1, pageSize },
    signal,
  );
}

/** Busca programas por nombre. Sin texto, devuelve los primeros. */
export function buscarProgramas(texto: string, pageSize: number, signal: AbortSignal) {
  return listarProgramas({ nombre: texto || undefined, page: 1, pageSize }, signal);
}
