import { useApiMutation } from "../../api/useApiMutation";
import { ErrorMessage } from "./ErrorMessage";

interface EstadoEntidadProps {
  id: string;
  nombre: string;
  activo: boolean;
  cambiar: (id: string, activo: boolean) => Promise<unknown>;
  onSaved: () => void;
}

export function EstadoEntidad({ id, nombre, activo, cambiar, onSaved }: EstadoEntidadProps) {
  const estado = useApiMutation(cambiar);
  async function alternar() {
    const resultado = await estado.run(id, !activo);
    if (resultado.ok) onSaved();
  }
  return (
    <>
      <span>{activo ? "Activo" : "Inactivo"}</span>{" "}
      <button
        type="button"
        className="btn"
        aria-label={`${activo ? "Inactivar" : "Reactivar"} ${nombre}`}
        disabled={estado.state.status === "loading"}
        onClick={alternar}
      >
        {estado.state.status === "loading" ? "Guardando…" : activo ? "Inactivar" : "Reactivar"}
      </button>
      {estado.state.status === "error" && <ErrorMessage error={estado.state.error} />}
    </>
  );
}
