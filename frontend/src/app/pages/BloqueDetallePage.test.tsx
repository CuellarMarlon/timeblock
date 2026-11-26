import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ApiError } from "../../api/errors";
import { BloqueDetallePage } from "./BloqueDetallePage";

const mocks = vi.hoisted(() => ({
  cargarCatalogos: vi.fn(),
  obtenerBloque: vi.fn(),
  asociarBloqueAPrograma: vi.fn(),
  desasociarBloqueDePrograma: vi.fn(),
  eliminarBloque: vi.fn(),
}));

vi.mock("../../api/catalogos", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/catalogos")>()),
  cargarCatalogos: mocks.cargarCatalogos,
}));
vi.mock("../../api/bloques", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/bloques")>()),
  obtenerBloque: mocks.obtenerBloque,
  asociarBloqueAPrograma: mocks.asociarBloqueAPrograma,
  desasociarBloqueDePrograma: mocks.desasociarBloqueDePrograma,
  eliminarBloque: mocks.eliminarBloque,
}));

const catalogos = {
  programas: [
    { id: "p1", nombre: "Ingeniería Informática", planEstudio: "2024" },
    { id: "p2", nombre: "Ciencia de la Computación", planEstudio: "2024" },
  ],
  asignaturas: [{ id: "a1", nombre: "Cálculo" }],
  coordinadores: [{ id: "c1", nombre: "Ana Pérez" }],
};

const bloque = {
  id: "b1",
  descripcion: "Derivadas",
  tipoActividad: "practica",
  nivelComplejidad: "bajo",
  asignaturaId: "a1",
  coordinadorId: "c1",
  programasIds: ["p1"],
};

function renderPagina(aviso?: string) {
  return render(
    <MemoryRouter initialEntries={[{ pathname: "/bloques/b1", state: aviso ? { aviso } : null }]}>
      <Routes>
        <Route path="/bloques/:bloqueId" element={<BloqueDetallePage />} />
        <Route path="/bloques" element={<p>Lista de bloques</p>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("BloqueDetallePage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.cargarCatalogos.mockResolvedValue(catalogos);
    mocks.obtenerBloque.mockResolvedValue(bloque);
  });

  it("muestra los datos del bloque con nombres legibles y sus programas", async () => {
    renderPagina();

    expect(screen.getByText("Cargando bloque…")).toBeInTheDocument();
    expect(await screen.findByText("Derivadas")).toBeInTheDocument();
    expect(screen.getByText("Cálculo")).toBeInTheDocument();
    expect(screen.getByText("Práctica")).toBeInTheDocument();
    expect(screen.getByText("Bajo")).toBeInTheDocument();
    expect(screen.getByText("Ana Pérez")).toBeInTheDocument();
    expect(screen.getByRole("listitem")).toHaveTextContent("Ingeniería Informática");
  });

  it("muestra el aviso de éxito recibido tras crear o editar", async () => {
    renderPagina("Bloque creado correctamente.");

    expect(await screen.findByText("Bloque creado correctamente.")).toBeInTheDocument();
  });

  it("asocia el bloque a otro programa, avisa y recarga el detalle", async () => {
    const user = userEvent.setup();
    mocks.asociarBloqueAPrograma.mockResolvedValue(undefined);
    mocks.obtenerBloque
      .mockResolvedValueOnce(bloque)
      .mockResolvedValueOnce({ ...bloque, programasIds: ["p1", "p2"] });
    renderPagina();

    await user.selectOptions(
      await screen.findByRole("combobox", { name: "Asociar a un programa" }),
      "p2",
    );
    await user.click(screen.getByRole("button", { name: "Asociar" }));

    expect(mocks.asociarBloqueAPrograma).toHaveBeenCalledWith("b1", "p2");
    expect(
      await screen.findByText("Bloque asociado al programa «Ciencia de la Computación»."),
    ).toBeInTheDocument();
    expect(
      await screen.findByText("El bloque ya está asociado a todos los programas."),
    ).toBeInTheDocument();
    expect(mocks.obtenerBloque).toHaveBeenCalledTimes(2);
  });

  it("muestra el error de la API al asociar un programa incompatible", async () => {
    const user = userEvent.setup();
    const detail = "La asignatura del bloque no pertenece al programa.";
    mocks.asociarBloqueAPrograma.mockRejectedValue(
      new ApiError(409, { title: "Conflicto", detail, status: 409 }),
    );
    renderPagina();

    await user.selectOptions(
      await screen.findByRole("combobox", { name: "Asociar a un programa" }),
      "p2",
    );
    await user.click(screen.getByRole("button", { name: "Asociar" }));

    expect(await screen.findByText(detail)).toBeInTheDocument();
  });

  it("muestra el error cuando el bloque no existe", async () => {
    mocks.obtenerBloque.mockRejectedValue(
      new ApiError(404, { title: "No encontrado", detail: "El bloque no existe.", status: 404 }),
    );
    renderPagina();

    expect(await screen.findByText("El bloque no existe.")).toBeInTheDocument();
  });

  it("cancela la eliminación sin llamar a la API", async () => {
    const user = userEvent.setup();
    renderPagina();
    await user.click(await screen.findByRole("button", { name: "Eliminar bloque" }));
    await user.click(screen.getByRole("button", { name: "Cancelar" }));
    expect(mocks.eliminarBloque).not.toHaveBeenCalled();
  });

  it("elimina un bloque y vuelve al listado", async () => {
    const user = userEvent.setup();
    mocks.eliminarBloque.mockResolvedValue(undefined);
    renderPagina();
    await user.click(await screen.findByRole("button", { name: "Eliminar bloque" }));
    await user.click(screen.getByRole("button", { name: "Confirmar eliminación" }));
    expect(mocks.eliminarBloque).toHaveBeenCalledWith("b1");
    expect(await screen.findByText("Lista de bloques")).toBeInTheDocument();
  });

  it("muestra el conflicto si hay dependencias", async () => {
    const user = userEvent.setup();
    mocks.eliminarBloque.mockRejectedValue(
      new ApiError(409, { detail: "El bloque tiene registros relacionados." }),
    );
    renderPagina();
    await user.click(await screen.findByRole("button", { name: "Eliminar bloque" }));
    await user.click(screen.getByRole("button", { name: "Confirmar eliminación" }));
    expect(await screen.findByText("El bloque tiene registros relacionados.")).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });

  it("impide cerrar mientras se elimina", async () => {
    const user = userEvent.setup();
    mocks.eliminarBloque.mockReturnValue(new Promise<void>(() => {}));
    renderPagina();
    await user.click(await screen.findByRole("button", { name: "Eliminar bloque" }));
    await user.click(screen.getByRole("button", { name: "Confirmar eliminación" }));
    expect(screen.getByRole("button", { name: "Eliminando…" })).toBeDisabled();
    await user.keyboard("{Escape}");
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });

  it("muestra el programa y permite cancelar sin desvincular", async () => {
    const user = userEvent.setup();
    renderPagina();
    await user.click(
      await screen.findByRole("button", { name: "Desvincular de Ingeniería Informática" }),
    );
    const dialogo = screen.getByRole("dialog", { name: "Confirmar desvinculación" });
    expect(within(dialogo).getByText("Ingeniería Informática")).toBeInTheDocument();
    await user.click(within(dialogo).getByRole("button", { name: "Cancelar" }));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(mocks.desasociarBloqueDePrograma).not.toHaveBeenCalled();
  });

  it("desvincula el programa elegido y recarga el bloque", async () => {
    const user = userEvent.setup();
    mocks.desasociarBloqueDePrograma.mockResolvedValue(undefined);
    mocks.obtenerBloque
      .mockResolvedValueOnce(bloque)
      .mockResolvedValue({ ...bloque, programasIds: [] });
    renderPagina();
    await user.click(
      await screen.findByRole("button", { name: "Desvincular de Ingeniería Informática" }),
    );
    await user.click(
      within(screen.getByRole("dialog")).getByRole("button", { name: "Confirmar desvinculación" }),
    );
    expect(mocks.desasociarBloqueDePrograma).toHaveBeenCalledWith("b1", "p1");
    expect(
      await screen.findByText("Bloque desvinculado del programa «Ingeniería Informática»."),
    ).toBeInTheDocument();
    expect(
      await screen.findByText("Este bloque todavía no está asociado a ningún programa."),
    ).toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("muestra el conflicto y conserva el programa asociado", async () => {
    const user = userEvent.setup();
    mocks.desasociarBloqueDePrograma.mockRejectedValue(
      new ApiError(409, { detail: "El bloque pertenece a un horario." }),
    );
    renderPagina();
    await user.click(
      await screen.findByRole("button", { name: "Desvincular de Ingeniería Informática" }),
    );
    const dialogo = screen.getByRole("dialog");
    await user.click(within(dialogo).getByRole("button", { name: "Confirmar desvinculación" }));
    expect(
      await within(dialogo).findByText("El bloque pertenece a un horario."),
    ).toBeInTheDocument();
    expect(screen.getByRole("listitem")).toHaveTextContent("Ingeniería Informática");
  });

  it("mantiene el modal abierto durante la petición", async () => {
    const user = userEvent.setup();
    mocks.desasociarBloqueDePrograma.mockReturnValue(new Promise<void>(() => {}));
    renderPagina();
    await user.click(
      await screen.findByRole("button", { name: "Desvincular de Ingeniería Informática" }),
    );
    const dialogo = screen.getByRole("dialog");
    await user.click(within(dialogo).getByRole("button", { name: "Confirmar desvinculación" }));
    expect(within(dialogo).getByRole("button", { name: "Desvinculando…" })).toBeDisabled();
    expect(within(dialogo).getByRole("button", { name: "Cancelar" })).toBeDisabled();
    await user.keyboard("{Escape}");
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(mocks.desasociarBloqueDePrograma).toHaveBeenCalledTimes(1);
  });
});
