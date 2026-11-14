import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ApiError } from "../../api/errors";
import { HorarioNuevoPage } from "./HorarioNuevoPage";

const mocks = vi.hoisted(() => ({
  cargarCatalogos: vi.fn(),
  listarBloques: vi.fn(),
  crearHorario: vi.fn(),
}));

vi.mock("../../api/catalogos", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/catalogos")>()),
  cargarCatalogos: mocks.cargarCatalogos,
}));
vi.mock("../../api/bloques", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/bloques")>()),
  listarBloques: mocks.listarBloques,
}));
vi.mock("../../api/horarios", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/horarios")>()),
  crearHorario: mocks.crearHorario,
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
    <MemoryRouter initialEntries={["/horarios/nuevo"]}>
      <Routes>
        <Route path="/horarios/nuevo" element={<HorarioNuevoPage />} />
        <Route path="/horarios/:horarioId" element={<p>Horario creado</p>} />
      </Routes>
    </MemoryRouter>,
  );
}

/** Rellena programa, periodo y coordinador, y espera a que carguen los bloques. */
async function rellenarCabecera(user: ReturnType<typeof userEvent.setup>) {
  await user.selectOptions(await screen.findByLabelText("Programa"), "p1");
  await user.type(screen.getByLabelText("Periodo académico"), "2026-2027");
  await user.selectOptions(screen.getByLabelText("Coordinador"), "c1");
  await screen.findByLabelText("Incluir Derivadas");
}

describe("HorarioNuevoPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.cargarCatalogos.mockResolvedValue(catalogos);
    mocks.listarBloques.mockResolvedValue({
      items: [bloque],
      page: 1,
      pageSize: 100,
      totalItems: 1,
    });
  });

  it("crea el horario con una fecha por bloque y sin enviar estado", async () => {
    const user = userEvent.setup();
    mocks.crearHorario.mockResolvedValue({ id: "h1" });
    renderPagina();

    await rellenarCabecera(user);
    await user.click(screen.getByLabelText("Incluir Derivadas"));
    fireEvent.change(screen.getByLabelText("Fecha de Derivadas"), {
      target: { value: "2026-11-20" },
    });
    await user.click(screen.getByRole("button", { name: "Crear horario" }));

    // La comparación es exacta: si se enviara estado u otro campo, fallaría.
    expect(mocks.crearHorario).toHaveBeenCalledWith({
      programaId: "p1",
      periodoAcademico: "2026-2027",
      coordinadorId: "c1",
      bloques: [{ bloqueId: "b1", fecha: "2026-11-20" }],
    });
    expect(await screen.findByText("Horario creado")).toBeInTheDocument();
  });

  it("no envía si a un bloque seleccionado le falta la fecha", async () => {
    const user = userEvent.setup();
    renderPagina();

    await rellenarCabecera(user);
    await user.click(screen.getByLabelText("Incluir Derivadas"));
    await user.click(screen.getByRole("button", { name: "Crear horario" }));

    expect(screen.getByText("Indica la fecha de cada bloque seleccionado.")).toBeInTheDocument();
    expect(screen.getByText("Falta la fecha.")).toBeInTheDocument();
    expect(mocks.crearHorario).not.toHaveBeenCalled();
  });

  it("valida los campos obligatorios antes de enviar", async () => {
    const user = userEvent.setup();
    renderPagina();

    await user.click(await screen.findByRole("button", { name: "Crear horario" }));

    expect(screen.getByText("Selecciona un programa.")).toBeInTheDocument();
    expect(screen.getByText("El periodo académico es obligatorio.")).toBeInTheDocument();
    expect(screen.getByText("Selecciona el coordinador.")).toBeInTheDocument();
    expect(screen.getByText("Selecciona al menos un bloque.")).toBeInTheDocument();
    expect(mocks.crearHorario).not.toHaveBeenCalled();
  });

  it("indica que el programa no tiene bloques", async () => {
    const user = userEvent.setup();
    mocks.listarBloques.mockResolvedValue({ items: [], page: 1, pageSize: 100, totalItems: 0 });
    renderPagina();

    await user.selectOptions(await screen.findByLabelText("Programa"), "p1");

    expect(await screen.findByText(/no tiene bloques asociados/)).toBeInTheDocument();
  });

  it.each([
    [403, "El coordinador no tiene autorización vigente para este programa."],
    [404, "El bloque indicado no existe."],
    [409, "Ya existe un horario con esos datos."],
  ])("muestra el error %i de la API sin salir del formulario", async (status, detail) => {
    const user = userEvent.setup();
    mocks.crearHorario.mockRejectedValue(new ApiError(status, { title: "Error", detail, status }));
    renderPagina();

    await rellenarCabecera(user);
    await user.click(screen.getByLabelText("Incluir Derivadas"));
    fireEvent.change(screen.getByLabelText("Fecha de Derivadas"), {
      target: { value: "2026-11-20" },
    });
    await user.click(screen.getByRole("button", { name: "Crear horario" }));

    expect(await screen.findByText(detail)).toBeInTheDocument();
    expect(screen.queryByText("Horario creado")).not.toBeInTheDocument();
  });
});
