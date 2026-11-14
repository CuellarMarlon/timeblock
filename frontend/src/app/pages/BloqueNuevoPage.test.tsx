import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ApiError } from "../../api/errors";
import { BloqueNuevoPage } from "./BloqueNuevoPage";

const mocks = vi.hoisted(() => ({
  cargarCatalogos: vi.fn(),
  crearBloque: vi.fn(),
}));

vi.mock("../../api/catalogos", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/catalogos")>()),
  cargarCatalogos: mocks.cargarCatalogos,
}));
vi.mock("../../api/bloques", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/bloques")>()),
  crearBloque: mocks.crearBloque,
}));

const catalogos = {
  programas: [{ id: "p1", nombre: "Ingeniería Informática", planEstudio: "2024" }],
  asignaturas: [{ id: "a1", nombre: "Cálculo" }],
  coordinadores: [{ id: "c1", nombre: "Ana Pérez" }],
};

function renderPagina() {
  return render(
    <MemoryRouter initialEntries={["/bloques/nuevo"]}>
      <Routes>
        <Route path="/bloques/nuevo" element={<BloqueNuevoPage />} />
        <Route path="/bloques/:bloqueId" element={<p>Bloque creado</p>} />
      </Routes>
    </MemoryRouter>,
  );
}

async function rellenarFormulario(user: ReturnType<typeof userEvent.setup>) {
  await user.type(await screen.findByLabelText("Descripción"), "  Derivadas  ");
  await user.selectOptions(screen.getByLabelText("Asignatura"), "a1");
  await user.selectOptions(screen.getByLabelText("Tipo de actividad"), "teoria");
  await user.selectOptions(screen.getByLabelText("Complejidad"), "medio");
  await user.selectOptions(screen.getByLabelText("Coordinador creador"), "c1");
}

describe("BloqueNuevoPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.cargarCatalogos.mockResolvedValue(catalogos);
  });

  it("crea el bloque asociado a los programas marcados", async () => {
    const user = userEvent.setup();
    mocks.crearBloque.mockResolvedValue({ id: "b1" });
    renderPagina();

    await rellenarFormulario(user);
    await user.click(screen.getByLabelText("Ingeniería Informática"));
    await user.click(screen.getByRole("button", { name: "Crear bloque" }));

    expect(mocks.crearBloque).toHaveBeenCalledWith({
      descripcion: "Derivadas",
      asignaturaId: "a1",
      tipoActividad: "teoria",
      nivelComplejidad: "medio",
      coordinadorId: "c1",
      programasIds: ["p1"],
    });
    expect(await screen.findByText("Bloque creado")).toBeInTheDocument();
  });

  it("valida los campos obligatorios sin llamar a la API", async () => {
    const user = userEvent.setup();
    renderPagina();

    await user.click(await screen.findByRole("button", { name: "Crear bloque" }));

    expect(screen.getByText("La descripción es obligatoria.")).toBeInTheDocument();
    expect(screen.getByText("Selecciona una asignatura.")).toBeInTheDocument();
    expect(mocks.crearBloque).not.toHaveBeenCalled();
  });

  it.each([
    [409, "Ya existe un bloque con la misma asignatura, actividad y descripción."],
    [400, "La asignatura no es compatible con el programa indicado."],
  ])("muestra el error %i de la API", async (status, detail) => {
    const user = userEvent.setup();
    mocks.crearBloque.mockRejectedValue(new ApiError(status, { title: "Error", detail, status }));
    renderPagina();

    await rellenarFormulario(user);
    await user.click(screen.getByRole("button", { name: "Crear bloque" }));

    expect(await screen.findByText(detail)).toBeInTheDocument();
    expect(screen.queryByText("Bloque creado")).not.toBeInTheDocument();
  });
});
