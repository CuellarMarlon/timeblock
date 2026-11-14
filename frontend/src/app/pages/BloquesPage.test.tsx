import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { ApiError } from "../../api/errors";
import { BloquesPage } from "./BloquesPage";

const mocks = vi.hoisted(() => ({
  cargarCatalogos: vi.fn(),
  listarBloques: vi.fn(),
}));

vi.mock("../../api/catalogos", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/catalogos")>()),
  cargarCatalogos: mocks.cargarCatalogos,
}));
vi.mock("../../api/bloques", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/bloques")>()),
  listarBloques: mocks.listarBloques,
}));

const catalogos = {
  programas: [{ id: "p1", nombre: "Ingeniería Informática", planEstudio: "2024" }],
  asignaturas: [{ id: "a1", nombre: "Cálculo" }],
  coordinadores: [{ id: "c1", nombre: "Ana Pérez" }],
};

const bloque = {
  id: "b1",
  descripcion: "Derivadas",
  tipoActividad: "laboratorio",
  nivelComplejidad: "alto",
  asignaturaId: "a1",
  coordinadorId: "c1",
  programasIds: ["p1"],
};

function pagina(items: unknown[]) {
  return { items, page: 1, pageSize: 20, totalItems: items.length };
}

function renderPagina() {
  return render(
    <MemoryRouter>
      <BloquesPage />
    </MemoryRouter>,
  );
}

describe("BloquesPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.cargarCatalogos.mockResolvedValue(catalogos);
  });

  it("muestra la carga y después los bloques con nombres legibles", async () => {
    mocks.listarBloques.mockResolvedValue(pagina([bloque]));
    renderPagina();

    expect(screen.getByText("Cargando bloques…")).toBeInTheDocument();

    expect(await screen.findByRole("link", { name: "Derivadas" })).toBeInTheDocument();
    const fila = screen.getByRole("row", { name: /Derivadas/ });
    expect(fila).toHaveTextContent("Cálculo");
    expect(fila).toHaveTextContent("Laboratorio");
    expect(fila).toHaveTextContent("Alto");
    expect(fila).toHaveTextContent("Ana Pérez");
    expect(fila).toHaveTextContent("Ingeniería Informática");
  });

  it("muestra el estado vacío", async () => {
    mocks.listarBloques.mockResolvedValue(pagina([]));
    renderPagina();

    expect(
      await screen.findByText("No hay bloques que coincidan con los filtros."),
    ).toBeInTheDocument();
  });

  it("muestra el error de la API y permite reintentar", async () => {
    const user = userEvent.setup();
    mocks.listarBloques
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce(pagina([bloque]));
    renderPagina();

    expect(await screen.findByRole("alert")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    expect(await screen.findByRole("link", { name: "Derivadas" })).toBeInTheDocument();
  });

  it("filtra por programa y por tipo de actividad volviendo a consultar la API", async () => {
    const user = userEvent.setup();
    mocks.listarBloques.mockResolvedValue(pagina([bloque]));
    renderPagina();
    await screen.findByRole("link", { name: "Derivadas" });

    await user.selectOptions(screen.getByLabelText("Programa"), "p1");
    await user.selectOptions(screen.getByLabelText("Tipo de actividad"), "laboratorio");

    const [filtros, paginacion] = mocks.listarBloques.mock.lastCall ?? [];
    expect(filtros).toEqual({ programaId: "p1", tipoActividad: "laboratorio" });
    expect(paginacion).toEqual({ page: 1, pageSize: 20 });
  });
});
