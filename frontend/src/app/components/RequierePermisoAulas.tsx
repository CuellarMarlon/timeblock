import type { ReactNode } from "react";
import { ApiError } from "../../api/errors";
import { usePermisosAulas } from "../permisosAulas";
import { ErrorMessage } from "./ErrorMessage";
import { LoadingIndicator } from "./LoadingIndicator";

interface RequierePermisoAulasProps {
  children: ReactNode;
}

export function RequierePermisoAulas({ children }: RequierePermisoAulasProps) {
  const permisos = usePermisosAulas();

  if (permisos.cargando) return <LoadingIndicator label="Comprobando permisos…" />;
  if (permisos.sinSesion) return <ErrorMessage error={new ApiError(401)} />;
  if (!permisos.puedeGestionar) return <ErrorMessage error={new ApiError(403)} />;

  return <>{children}</>;
}
