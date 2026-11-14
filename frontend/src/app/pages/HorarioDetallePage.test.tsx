import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ApiError } from "../../api/errors";
import { HorarioDetallePage } from "./HorarioDetallePage";

const mocks = vi.hoisted(() => ({
  cargarCatalogos: vi.fn(),
  obtenerHorario: vi.fn(),
}));

vi.mock("../../api/catalogos", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/catalogos")>()),
  cargarCatalogos: mocks.cargarCatalogos,
}));
vi.mock("../../api/horarios", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/horarios")>()),
  obtenerHorario: mocks.obtenerHorario,
}));

const catalogos = {
  programas: [{ id: "p1", nombre: "Ingeniería Informática", planEstudio: "2024" }],
  asignaturas: [{ id: "a1", nombre: "Cálculo" }],
  coordinadores: [{ id: "c1", nombre: "Ana Pérez" }],
};

const bloqueEnHorario = {
  bloqueId: "b1",
  descripcion: "Derivadas",
  asignaturaId: "a1",
  tipoActividad: "teoria",
  nivelComplejidad: "medio",
  horaInicio: null,
  horaFin: null,
  periodoImp: null,
  aulaId: null,
};

const horario = {
  id: "h1",
  programaId: "p1",
  periodoAcademico: "2026-2027",
  coordinadorId: "c1",
  fechaInicioAutorizacion: "2026-09-01",
  fechaCreacion: "2026-10-01T12:00:00Z",
  estado: "generado",
  cantTotalBloques: 1,
  // Un mismo bloque puede aparecer en varias fechas.
  bloques: [
    { ...bloqueEnHorario, fecha: "2026-11-20" },
    { ...bloqueEnHorario, fecha: "2026-11-27" },
  ],
};

function renderPagina(aviso?: string) {
  return render(
    <MemoryRouter initialEntries={[{ pathname: "/horarios/h1", state: aviso ? { aviso } : null }]}>
      <Routes>
        <Route path="/horarios/:horarioId" element={<HorarioDetallePage />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("HorarioDetallePage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.cargarCatalogos.mockResolvedValue(catalogos);
    mocks.obtenerHorario.mockResolvedValue(horario);
  });

  it("muestra los datos del horario, su estado GENERADO y cada bloque con su fecha", async () => {
    renderPagina();

    expect(screen.getByText("Cargando horario…")).toBeInTheDocument();
    expect(await screen.findByText("GENERADO")).toBeInTheDocument();
    expect(screen.getByText("2026-2027")).toBeInTheDocument();
    expect(screen.getByText("Ingeniería Informática")).toBeInTheDocument();
    expect(screen.getByText("Ana Pérez")).toBeInTheDocument();

    expect(screen.getByRole("row", { name: /2026-11-20/ })).toHaveTextContent("Derivadas");
    expect(screen.getByRole("row", { name: /2026-11-27/ })).toHaveTextContent("Cálculo");
    expect(screen.getAllByRole("link", { name: "Derivadas" })).toHaveLength(2);
  });

  it("muestra el aviso de éxito tras crear el horario", async () => {
    renderPagina("Horario creado correctamente con estado GENERADO.");

    expect(
      await screen.findByText("Horario creado correctamente con estado GENERADO."),
    ).toBeInTheDocument();
  });

  it("indica cuando el horario no tiene bloques", async () => {
    mocks.obtenerHorario.mockResolvedValue({ ...horario, cantTotalBloques: 0, bloques: [] });
    renderPagina();

    expect(await screen.findByText("Este horario no tiene bloques asociados.")).toBeInTheDocument();
  });

  it("muestra el error cuando el horario no existe", async () => {
    mocks.obtenerHorario.mockRejectedValue(
      new ApiError(404, { title: "No encontrado", detail: "El horario no existe.", status: 404 }),
    );
    renderPagina();

    expect(await screen.findByText("El horario no existe.")).toBeInTheDocument();
  });
});
