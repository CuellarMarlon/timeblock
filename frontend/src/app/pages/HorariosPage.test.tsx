import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { ApiError } from "../../api/errors";
import { HorariosPage } from "./HorariosPage";

const mocks = vi.hoisted(() => ({
  cargarCatalogos: vi.fn(),
  listarHorarios: vi.fn(),
}));

vi.mock("../../api/catalogos", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/catalogos")>()),
  cargarCatalogos: mocks.cargarCatalogos,
}));
vi.mock("../../api/horarios", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/horarios")>()),
  listarHorarios: mocks.listarHorarios,
}));

const catalogos = {
  programas: [{ id: "p1", nombre: "Ingeniería Informática", planEstudio: "2024" }],
  asignaturas: [{ id: "a1", nombre: "Cálculo" }],
  coordinadores: [{ id: "c1", nombre: "Ana Pérez" }],
};

const horario = {
  id: "h1",
  programaId: "p1",
  periodoAcademico: "2026-2027",
  coordinadorId: "c1",
  fechaInicioAutorizacion: "2026-09-01",
  fechaCreacion: "2026-10-01T12:00:00Z",
  estado: "generado",
  cantTotalBloques: 2,
  bloques: [],
};

function pagina(items: unknown[]) {
  return { items, page: 1, pageSize: 20, totalItems: items.length };
}

function renderPagina() {
  return render(
    <MemoryRouter>
      <HorariosPage />
    </MemoryRouter>,
  );
}

describe("HorariosPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.cargarCatalogos.mockResolvedValue(catalogos);
  });

  it("muestra la carga y después los horarios con su estado GENERADO", async () => {
    mocks.listarHorarios.mockResolvedValue(pagina([horario]));
    renderPagina();

    expect(screen.getByText("Cargando horarios…")).toBeInTheDocument();

    const fila = await screen.findByRole("row", { name: /2026-2027/ });
    expect(fila).toHaveTextContent("Ingeniería Informática");
    expect(fila).toHaveTextContent("Ana Pérez");
    expect(fila).toHaveTextContent("GENERADO");
    expect(fila).toHaveTextContent("2");
    expect(
      screen.getByRole("link", { name: "Ver horario 2026-2027 de Ingeniería Informática" }),
    ).toHaveAttribute("href", "/horarios/h1");
  });

  it("muestra el estado vacío", async () => {
    mocks.listarHorarios.mockResolvedValue(pagina([]));
    renderPagina();

    expect(
      await screen.findByText("No hay horarios que coincidan con los filtros."),
    ).toBeInTheDocument();
  });

  it("muestra el error de la API y permite reintentar", async () => {
    const user = userEvent.setup();
    mocks.listarHorarios
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce(pagina([horario]));
    renderPagina();

    expect(await screen.findByRole("alert")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    expect(await screen.findByRole("row", { name: /2026-2027/ })).toBeInTheDocument();
  });

  it("filtra por programa y por periodo académico", async () => {
    const user = userEvent.setup();
    mocks.listarHorarios.mockResolvedValue(pagina([horario]));
    renderPagina();
    await screen.findByRole("row", { name: /2026-2027/ });

    await user.selectOptions(screen.getByLabelText("Programa"), "p1");
    await user.type(screen.getByLabelText("Periodo académico"), "2026-2027");
    await user.click(screen.getByRole("button", { name: "Filtrar" }));

    const [filtros, paginacion] = mocks.listarHorarios.mock.lastCall ?? [];
    expect(filtros).toEqual({ programaId: "p1", periodoAcademico: "2026-2027" });
    expect(paginacion).toEqual({ page: 1, pageSize: 20 });
  });
});
