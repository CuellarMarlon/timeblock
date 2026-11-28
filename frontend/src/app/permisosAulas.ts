import { ApiError } from "../api/errors";
import { obtenerSesion } from "../api/sesion";
import { useApiQuery } from "../api/useApiQuery";

export interface PermisosAulas {
  cargando: boolean;
  sinSesion: boolean;
  puedeConsultar: boolean;
  puedeGestionar: boolean;
}

export function usePermisosAulas(): PermisosAulas {
  const sesion = useApiQuery(obtenerSesion);

  if (sesion.status === "loading") {
    return { cargando: true, sinSesion: false, puedeConsultar: false, puedeGestionar: false };
  }

  if (sesion.status === "success") {
    return {
      cargando: false,
      sinSesion: false,
      puedeConsultar: true,
      puedeGestionar: sesion.data.rol === "administrador",
    };
  }

  const estado = sesion.error instanceof ApiError ? sesion.error.status : 0;

  if (estado === 401) {
    return { cargando: false, sinSesion: true, puedeConsultar: false, puedeGestionar: false };
  }

  if (estado === 404) {
    return { cargando: false, sinSesion: false, puedeConsultar: true, puedeGestionar: true };
  }

  return { cargando: false, sinSesion: false, puedeConsultar: true, puedeGestionar: false };
}
