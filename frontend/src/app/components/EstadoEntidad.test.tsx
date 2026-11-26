import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ApiError } from "../../api/errors";
import { EstadoEntidad } from "./EstadoEntidad";

describe("EstadoEntidad", () => {
  it("inactiva y solicita recargar", async () => {
    const cambiar = vi.fn().mockResolvedValue({});
    const onSaved = vi.fn();
    render(<EstadoEntidad id="1" nombre="Programa" activo cambiar={cambiar} onSaved={onSaved} />);
    await userEvent.click(screen.getByRole("button", { name: "Inactivar Programa" }));
    await waitFor(() => expect(cambiar).toHaveBeenCalledWith("1", false));
    expect(onSaved).toHaveBeenCalledOnce();
  });

  it("reactiva un registro inactivo", async () => {
    const cambiar = vi.fn().mockResolvedValue({});
    render(
      <EstadoEntidad id="1" nombre="Programa" activo={false} cambiar={cambiar} onSaved={vi.fn()} />,
    );
    await userEvent.click(screen.getByRole("button", { name: "Reactivar Programa" }));
    await waitFor(() => expect(cambiar).toHaveBeenCalledWith("1", true));
  });

  it("muestra el error sin recargar", async () => {
    const cambiar = vi.fn().mockRejectedValue(new ApiError(409, { detail: "No se pudo cambiar." }));
    const onSaved = vi.fn();
    render(<EstadoEntidad id="1" nombre="Programa" activo cambiar={cambiar} onSaved={onSaved} />);
    await userEvent.click(screen.getByRole("button", { name: "Inactivar Programa" }));
    expect(await screen.findByText("No se pudo cambiar.")).toBeInTheDocument();
    expect(onSaved).not.toHaveBeenCalled();
  });
});
