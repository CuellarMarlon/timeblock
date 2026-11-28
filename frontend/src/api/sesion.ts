import { apiClient } from "./client";

export type Rol = "administrador" | "estudiante" | "coordinador" | "jefeDepartamento";

export interface Sesion {
  cuentaId: string;
  usuario: string;
  rol: Rol;
  personaId: string | null;
  nombre: string | null;
}

export function obtenerSesion(signal?: AbortSignal): Promise<Sesion> {
  return apiClient.get<Sesion>("/api/sesion", { signal });
}
