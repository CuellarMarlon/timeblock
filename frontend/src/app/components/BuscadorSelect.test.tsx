import { render, screen } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import { BuscadorSelect } from "./BuscadorSelect";

it("impide elegir registros inactivos para crear relaciones", async () => {
  const buscar = vi.fn().mockResolvedValue({
    items: [
      { id: "1", nombre: "Ana", activo: false },
      { id: "2", nombre: "Luis", activo: true },
    ],
    page: 1,
    pageSize: 20,
    totalItems: 2,
  });
  render(
    <BuscadorSelect
      label="Estudiante"
      searchLabel="Buscar"
      placeholder="Selecciona"
      value=""
      onChange={vi.fn()}
      buscar={buscar}
    />,
  );
  expect(await screen.findByRole("option", { name: "Ana (inactivo)" })).toBeDisabled();
  expect(screen.getByRole("option", { name: "Luis" })).toBeEnabled();
});
