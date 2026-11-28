import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { ApiError } from "../../api/errors";
import { AulaEditarPage } from "./AulaEditarPage";

const mocks = vi.hoisted(() => ({ obtenerAula: vi.fn(), actualizarAula: vi.fn() }));

vi.mock("../../api/aulas", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/aulas")>()),
  obtenerAula: mocks.obtenerAula,
  actualizarAula: mocks.actualizarAula,
}));

const aula = { id: "a1", edificio: "Edificio A", capacidad: 30 };

function Listado() {
  const aviso = (useLocation().state as { aviso?: string } | null)?.aviso;
  return <p>Listado de aulas. {aviso}</p>;
}

function renderPagina() {
  return render(
    <MemoryRouter initialEntries={["/aulas/a1/editar"]}>
      <Routes>
        <Route path="/aulas/:aulaId/editar" element={<AulaEditarPage />} />
        <Route path="/aulas" element={<Listado />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("AulaEditarPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it("carga el aula y muestra sus datos en el formulario", async () => {
    mocks.obtenerAula.mockResolvedValue(aula);
    renderPagina();

    expect(screen.getByText("Cargando aula…")).toBeInTheDocument();
    expect(await screen.findByLabelText("Edificio")).toHaveValue("Edificio A");
    expect(screen.getByLabelText("Capacidad")).toHaveValue(30);
    expect(mocks.obtenerAula.mock.calls[0][0]).toBe("a1");
  });

  it("guarda los cambios y vuelve al listado con un aviso", async () => {
    const user = userEvent.setup();
    mocks.obtenerAula.mockResolvedValue(aula);
    mocks.actualizarAula.mockResolvedValue({ ...aula, capacidad: 45 });
    renderPagina();

    const capacidad = await screen.findByLabelText("Capacidad");
    await user.clear(capacidad);
    await user.type(capacidad, "45");
    await user.click(screen.getByRole("button", { name: "Guardar cambios" }));

    expect(mocks.actualizarAula).toHaveBeenCalledWith("a1", {
      edificio: "Edificio A",
      capacidad: 45,
    });
    expect(
      await screen.findByText("Listado de aulas. Aula actualizada correctamente."),
    ).toBeInTheDocument();
  });

  it("no llama a la API si no se cambió nada", async () => {
    const user = userEvent.setup();
    mocks.obtenerAula.mockResolvedValue(aula);
    renderPagina();

    await user.click(await screen.findByRole("button", { name: "Guardar cambios" }));

    expect(screen.getByText("No hay cambios que guardar.")).toBeInTheDocument();
    expect(mocks.actualizarAula).not.toHaveBeenCalled();
  });

  it("valida igual que al crear", async () => {
    const user = userEvent.setup();
    mocks.obtenerAula.mockResolvedValue(aula);
    renderPagina();

    await user.clear(await screen.findByLabelText("Edificio"));
    await user.click(screen.getByRole("button", { name: "Guardar cambios" }));

    expect(screen.getByText("El edificio es obligatorio.")).toBeInTheDocument();
    expect(mocks.actualizarAula).not.toHaveBeenCalled();
  });

  it("muestra el error de la API si el aula ya no existe al guardar", async () => {
    const user = userEvent.setup();
    mocks.obtenerAula.mockResolvedValue(aula);
    mocks.actualizarAula.mockRejectedValue(new ApiError(404));
    renderPagina();

    const edificio = await screen.findByLabelText("Edificio");
    await user.type(edificio, " B");
    await user.click(screen.getByRole("button", { name: "Guardar cambios" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("El recurso solicitado no existe.");
  });

  it("muestra el error cuando el aula no se puede cargar y permite reintentar", async () => {
    const user = userEvent.setup();
    mocks.obtenerAula.mockRejectedValueOnce(new ApiError(404)).mockResolvedValueOnce(aula);
    renderPagina();

    expect(await screen.findByRole("alert")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    expect(await screen.findByLabelText("Edificio")).toHaveValue("Edificio A");
  });
});
