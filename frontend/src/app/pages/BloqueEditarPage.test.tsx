import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ApiError } from "../../api/errors";
import { BloqueEditarPage } from "./BloqueEditarPage";

const mocks = vi.hoisted(() => ({
  cargarCatalogos: vi.fn(),
  obtenerBloque: vi.fn(),
  actualizarBloque: vi.fn(),
}));

vi.mock("../../api/catalogos", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/catalogos")>()),
  cargarCatalogos: mocks.cargarCatalogos,
}));
vi.mock("../../api/bloques", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/bloques")>()),
  obtenerBloque: mocks.obtenerBloque,
  actualizarBloque: mocks.actualizarBloque,
}));

const catalogos = {
  programas: [{ id: "p1", nombre: "Ingeniería Informática", planEstudio: "2024" }],
  asignaturas: [{ id: "a1", nombre: "Cálculo" }],
  coordinadores: [{ id: "c1", nombre: "Ana Pérez" }],
};

const bloque = {
  id: "b1",
  descripcion: "Derivadas",
  tipoActividad: "teoria",
  nivelComplejidad: "medio",
  asignaturaId: "a1",
  coordinadorId: "c1",
  programasIds: ["p1"],
};

function renderPagina() {
  return render(
    <MemoryRouter initialEntries={["/bloques/b1/editar"]}>
      <Routes>
        <Route path="/bloques/:bloqueId/editar" element={<BloqueEditarPage />} />
        <Route path="/bloques/:bloqueId" element={<p>Bloque actualizado</p>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("BloqueEditarPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.cargarCatalogos.mockResolvedValue(catalogos);
    mocks.obtenerBloque.mockResolvedValue(bloque);
  });

  it("precarga los datos actuales del bloque", async () => {
    renderPagina();

    expect(await screen.findByLabelText("Descripción")).toHaveValue("Derivadas");
    expect(screen.getByLabelText("Asignatura")).toHaveValue("a1");
    expect(screen.getByLabelText("Tipo de actividad")).toHaveValue("teoria");
    expect(screen.getByLabelText("Complejidad")).toHaveValue("medio");
    expect(screen.getByText("Ana Pérez")).toBeInTheDocument();
  });

  it("envía solo los campos modificados", async () => {
    const user = userEvent.setup();
    mocks.actualizarBloque.mockResolvedValue({ ...bloque, nivelComplejidad: "alto" });
    renderPagina();

    await user.selectOptions(await screen.findByLabelText("Complejidad"), "alto");
    await user.click(screen.getByRole("button", { name: "Guardar cambios" }));

    expect(mocks.actualizarBloque).toHaveBeenCalledWith("b1", { nivelComplejidad: "alto" });
    expect(await screen.findByText("Bloque actualizado")).toBeInTheDocument();
  });

  it("no llama a la API si no hay cambios", async () => {
    const user = userEvent.setup();
    renderPagina();

    await user.click(await screen.findByRole("button", { name: "Guardar cambios" }));

    expect(screen.getByText("No hay cambios que guardar.")).toBeInTheDocument();
    expect(mocks.actualizarBloque).not.toHaveBeenCalled();
  });

  it("muestra el error de duplicidad de la API", async () => {
    const user = userEvent.setup();
    const detail = "Ya existe un bloque con la misma asignatura, actividad y descripción.";
    mocks.actualizarBloque.mockRejectedValue(
      new ApiError(409, { title: "Conflicto", detail, status: 409 }),
    );
    renderPagina();

    const descripcion = await screen.findByLabelText("Descripción");
    await user.clear(descripcion);
    await user.type(descripcion, "Integrales");
    await user.click(screen.getByRole("button", { name: "Guardar cambios" }));

    expect(await screen.findByText(detail)).toBeInTheDocument();
    expect(screen.queryByText("Bloque actualizado")).not.toBeInTheDocument();
  });
});
