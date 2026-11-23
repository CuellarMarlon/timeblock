import type { Perfil, TipoPerfil } from "../../api/persons";

/** Etiqueta legible de cada tipo de perfil (como en el diseño). */
export const TIPO_LABEL: Record<TipoPerfil, string> = {
  estudiante: "Estudiante",
  coordinador: "Coordinador académico",
  jefeDepartamento: "Jefe de departamento",
};

/**
 * Texto de detalle según el perfil.
 */
export function detallePerfil(perfil: Perfil, nombreCoordinador?: string): string {
  switch (perfil.tipo) {
    case "estudiante":
      return `${perfil.edad} años · ${perfil.grupo} · ${nombreCoordinador ?? "Coordinador desconocido"}`;
    case "coordinador":
      return perfil.especialidad;
    case "jefeDepartamento":
      return "—";
  }
}
