import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ApiError } from "../../api/errors";
import * as enrollments from "../../api/enrollments";
import * as persons from "../../api/persons";
import * as program from "../../api/program";
import { MatriculaPage } from "./MatriculaPage";

vi.mock("../../api/enrollments");
vi.mock("../../api/persons");
vi.mock("../../api/program");

const mEnrollments = vi.mocked(enrollments);
const mPersons = vi.mocked(persons);
const mProgram = vi.mocked(program);

function pagina<T>(items: T[]) {
  return { items, page: 1, pageSize: 10, totalItems: items.length };
}

const luis: persons.Persona = {
  id: "e1",
  nombre: "Luis",
  perfil: { tipo: "estudiante", edad: 20, grupo: "C311", coordinadorId: "c1" },
};
const ana: persons.Persona = {
  id: "c1",
  nombre: "Ana",
  perfil: { tipo: "coordinador", especialidad: "Informática" },
};
const ingenieria: program.Programa = { id: "p1", nombre: "Ingeniería", planEstudio: "2026" };
const matricula: enrollments.Matricula = { estudianteId: "e1", programaId: "p1" };

const nombres = new Map<string, string>([
  [luis.id, luis.nombre],
  [ana.id, ana.nombre],
  [ingenieria.id, ingenieria.nombre],
]);

async function abrirModal(user: ReturnType<typeof userEvent.setup>) {
  await user.click(await screen.findByRole("button", { name: "+ Nueva matrícula" }));
  return screen.getByRole("dialog");
}

/** Elige una opción del selector cuando la búsqueda ya la ha cargado. */
async function elegir(
  user: ReturnType<typeof userEvent.setup>,
  select: HTMLElement,
  opcion: string,
) {
  await within(select).findByRole("option", { name: opcion });
  await user.selectOptions(select, within(select).getByRole("option", { name: opcion }));
}

describe("MatriculaPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mPersons.listarPersonas.mockResolvedValue(pagina([luis, ana]));
    mProgram.listarProgramas.mockResolvedValue(pagina([ingenieria]));
    mEnrollments.listarMatriculas.mockResolvedValue(pagina([matricula]));
    mPersons.obtenerPersona.mockImplementation(async (id) => ({
      ...luis,
      id,
      nombre: nombres.get(id)!,
    }));
    mProgram.obtenerPrograma.mockImplementation(async (id) => ({
      ...ingenieria,
      id,
      nombre: nombres.get(id)!,
    }));
  });

  it("muestra el estado de carga", () => {
    mEnrollments.listarMatriculas.mockReturnValue(new Promise(() => {}));

    render(<MatriculaPage />);

    expect(screen.getByText("Cargando matrículas…")).toBeInTheDocument();
  });

  it("muestra las matrículas con los nombres resueltos", async () => {
    render(<MatriculaPage />);

    expect(await screen.findByRole("cell", { name: "Luis" })).toBeInTheDocument();
    expect(screen.getByRole("cell", { name: "Ingeniería" })).toBeInTheDocument();
  });

  it("muestra el estado vacío", async () => {
    mEnrollments.listarMatriculas.mockResolvedValue(pagina([]));

    render(<MatriculaPage />);

    expect(
      await screen.findByText("No hay matrículas para los filtros seleccionados."),
    ).toBeInTheDocument();
  });

  it("muestra el error de la API y permite reintentar", async () => {
    mEnrollments.listarMatriculas.mockRejectedValueOnce(
      new ApiError(500, { detail: "Fallo del servidor" }),
    );
    const user = userEvent.setup();

    render(<MatriculaPage />);

    expect(await screen.findByRole("alert")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Reintentar" }));
    expect(await screen.findByRole("cell", { name: "Luis" })).toBeInTheDocument();
  });

  it("filtra por estudiante y por programa", async () => {
    const user = userEvent.setup();

    render(<MatriculaPage />);
    await screen.findByRole("cell", { name: "Luis" });
    await elegir(user, screen.getByLabelText("Filtrar por estudiante"), "Luis");

    await waitFor(() =>
      expect(mEnrollments.listarMatriculas).toHaveBeenLastCalledWith(
        { estudianteId: "e1", programaId: undefined },
        { page: 1, pageSize: 10 },
        expect.any(AbortSignal),
      ),
    );

    await elegir(user, screen.getByLabelText("Filtrar por programa"), "Ingeniería");

    await waitFor(() =>
      expect(mEnrollments.listarMatriculas).toHaveBeenLastCalledWith(
        { estudianteId: "e1", programaId: "p1" },
        { page: 1, pageSize: 10 },
        expect.any(AbortSignal),
      ),
    );
  });

  it("valida la selección sin llamar a la API", async () => {
    const user = userEvent.setup();

    render(<MatriculaPage />);
    const dialogo = await abrirModal(user);
    await user.click(within(dialogo).getByRole("button", { name: "Matricular" }));

    expect(screen.getByText("Selecciona un estudiante.")).toBeInTheDocument();
    expect(screen.getByText("Selecciona un programa.")).toBeInTheDocument();
    expect(mEnrollments.matricular).not.toHaveBeenCalled();
  });

  it("advierte si el estudiante ya está matriculado y no envía", async () => {
    const user = userEvent.setup();

    render(<MatriculaPage />);
    const dialogo = await abrirModal(user);
    await elegir(user, within(dialogo).getByLabelText("Estudiante"), "Luis");
    await elegir(user, within(dialogo).getByLabelText("Programa académico"), "Ingeniería");
    await user.click(within(dialogo).getByRole("button", { name: "Matricular" }));

    expect(
      await screen.findByText("Este estudiante ya está matriculado en ese programa."),
    ).toBeInTheDocument();
    expect(mEnrollments.matricular).not.toHaveBeenCalled();
  });

  it("matricula, cierra el modal y recarga la tabla", async () => {
    mEnrollments.listarMatriculas
      .mockResolvedValueOnce(pagina([]))
      .mockResolvedValueOnce(pagina([]))
      .mockResolvedValue(pagina([matricula]));
    mEnrollments.matricular.mockResolvedValue(matricula);
    const user = userEvent.setup();

    render(<MatriculaPage />);
    const dialogo = await abrirModal(user);
    await elegir(user, within(dialogo).getByLabelText("Estudiante"), "Luis");
    await elegir(user, within(dialogo).getByLabelText("Programa académico"), "Ingeniería");
    await user.click(within(dialogo).getByRole("button", { name: "Matricular" }));

    await waitFor(() => expect(mEnrollments.matricular).toHaveBeenCalledWith("p1", "e1"));
    expect(await screen.findByText("Estudiante matriculado correctamente.")).toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(await screen.findByRole("cell", { name: "Luis" })).toBeInTheDocument();
  });

  it("muestra el error de la API al matricular y mantiene el modal", async () => {
    mEnrollments.listarMatriculas
      .mockResolvedValueOnce(pagina([]))
      .mockResolvedValueOnce(pagina([]));
    mEnrollments.matricular.mockRejectedValue(
      new ApiError(409, { detail: "La matrícula ya existe." }),
    );
    const user = userEvent.setup();

    render(<MatriculaPage />);
    const dialogo = await abrirModal(user);
    await elegir(user, within(dialogo).getByLabelText("Estudiante"), "Luis");
    await elegir(user, within(dialogo).getByLabelText("Programa académico"), "Ingeniería");
    await user.click(within(dialogo).getByRole("button", { name: "Matricular" }));

    expect(await screen.findByText("La matrícula ya existe.")).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });
});

describe("MatriculaPage con más de 100 registros", () => {
  const numero = (n: number) => String(n).padStart(3, "0");
  const estudiantes: persons.Persona[] = Array.from({ length: 150 }, (_, i) => ({
    id: `e${i + 1}`,
    nombre: `Estudiante ${numero(i + 1)}`,
    perfil: { tipo: "estudiante", edad: 20, grupo: "C311", coordinadorId: "c1" },
  }));
  const programas: program.Programa[] = Array.from({ length: 120 }, (_, i) => ({
    id: `p${i + 1}`,
    nombre: `Programa ${numero(i + 1)}`,
    planEstudio: "2026",
  }));

  /** Simula el filtro por nombre de la API: sin distinguir mayúsculas y paginado. */
  function filtrar<T extends { nombre: string }>(
    registros: T[],
    params: { nombre?: string; page?: number; pageSize?: number },
  ) {
    const texto = params.nombre?.toLowerCase() ?? "";
    const encontrados = registros.filter((r) => r.nombre.toLowerCase().includes(texto));
    const pageSize = params.pageSize ?? 20;
    const page = params.page ?? 1;
    return {
      items: encontrados.slice((page - 1) * pageSize, page * pageSize),
      page,
      pageSize,
      totalItems: encontrados.length,
    };
  }

  const ultima: enrollments.Matricula = { estudianteId: "e150", programaId: "p120" };

  beforeEach(() => {
    vi.resetAllMocks();
    mPersons.listarPersonas.mockImplementation(async (params) => filtrar(estudiantes, params));
    mProgram.listarProgramas.mockImplementation(async (params) => filtrar(programas, params));
    mPersons.obtenerPersona.mockImplementation(async (id) => estudiantes.find((e) => e.id === id)!);
    mProgram.obtenerPrograma.mockImplementation(async (id) => programas.find((p) => p.id === id)!);
    mEnrollments.listarMatriculas.mockResolvedValue(pagina([ultima]));
  });

  it("pide los estudiantes al servidor filtrando por perfil y por nombre", async () => {
    const user = userEvent.setup();

    render(<MatriculaPage />);
    expect(
      await screen.findAllByText("Mostrando 20 de 150. Escribe para afinar la búsqueda."),
    ).not.toHaveLength(0);
    await user.type(screen.getByLabelText("Buscar estudiante"), "150");

    await waitFor(() =>
      expect(mPersons.listarPersonas).toHaveBeenLastCalledWith(
        { nombre: "150", tipoPerfil: "estudiante", page: 1, pageSize: 20 },
        expect.any(AbortSignal),
      ),
    );
  });

  it("filtra por un estudiante y un programa posteriores a los primeros 100", async () => {
    const user = userEvent.setup();

    render(<MatriculaPage />);
    await screen.findByRole("cell", { name: "Estudiante 150" });
    await user.type(screen.getByLabelText("Buscar estudiante"), "estudiante 150");
    await elegir(user, screen.getByLabelText("Filtrar por estudiante"), "Estudiante 150");
    await user.type(screen.getByLabelText("Buscar programa"), "120");
    await elegir(user, screen.getByLabelText("Filtrar por programa"), "Programa 120");

    await waitFor(() =>
      expect(mEnrollments.listarMatriculas).toHaveBeenLastCalledWith(
        { estudianteId: "e150", programaId: "p120" },
        { page: 1, pageSize: 10 },
        expect.any(AbortSignal),
      ),
    );
  });

  it("mantiene la opción elegida aunque una búsqueda posterior no la incluya", async () => {
    const user = userEvent.setup();

    render(<MatriculaPage />);
    const filtro = screen.getByLabelText("Filtrar por estudiante");
    await user.type(screen.getByLabelText("Buscar estudiante"), "150");
    await elegir(user, filtro, "Estudiante 150");
    await user.clear(screen.getByLabelText("Buscar estudiante"));
    await user.type(screen.getByLabelText("Buscar estudiante"), "001");
    await within(filtro).findByRole("option", { name: "Estudiante 001" });

    expect(filtro).toHaveValue("e150");
    expect(within(filtro).getByRole("option", { name: "Estudiante 150" })).toBeInTheDocument();
  });

  it("resuelve los nombres de la tabla por id, una sola vez por registro", async () => {
    mEnrollments.listarMatriculas.mockResolvedValue(
      pagina([ultima, { estudianteId: "e150", programaId: "p101" }]),
    );

    render(<MatriculaPage />);

    expect(await screen.findAllByRole("cell", { name: "Estudiante 150" })).toHaveLength(2);
    expect(screen.getByRole("cell", { name: "Programa 120" })).toBeInTheDocument();
    expect(screen.getByRole("cell", { name: "Programa 101" })).toBeInTheDocument();
    expect(mPersons.obtenerPersona).toHaveBeenCalledTimes(1);
    expect(mProgram.obtenerPrograma).toHaveBeenCalledTimes(2);
  });

  it("muestra «desconocido» solo si el registro ya no existe", async () => {
    mPersons.obtenerPersona.mockRejectedValue(new ApiError(404, { detail: "No existe." }));

    render(<MatriculaPage />);

    expect(await screen.findByRole("cell", { name: "Estudiante desconocido" })).toBeInTheDocument();
    expect(screen.getByRole("cell", { name: "Programa 120" })).toBeInTheDocument();
  });

  it("muestra el error si no se pueden consultar los nombres y permite reintentar", async () => {
    mPersons.obtenerPersona.mockRejectedValueOnce(
      new ApiError(500, { detail: "Fallo del servidor" }),
    );
    const user = userEvent.setup();

    render(<MatriculaPage />);

    expect(await screen.findByText("Fallo del servidor")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Reintentar" }));
    expect(await screen.findByRole("cell", { name: "Estudiante 150" })).toBeInTheDocument();
  });

  it("matricula a un estudiante y un programa posteriores a los primeros 100", async () => {
    mEnrollments.listarMatriculas.mockResolvedValue(pagina([]));
    mEnrollments.matricular.mockResolvedValue(ultima);
    const user = userEvent.setup();

    render(<MatriculaPage />);
    const dialogo = await abrirModal(user);
    await user.type(within(dialogo).getByLabelText("Buscar estudiante"), "150");
    await elegir(user, within(dialogo).getByLabelText("Estudiante"), "Estudiante 150");
    await user.type(within(dialogo).getByLabelText("Buscar programa"), "120");
    await elegir(user, within(dialogo).getByLabelText("Programa académico"), "Programa 120");
    await user.click(within(dialogo).getByRole("button", { name: "Matricular" }));

    await waitFor(() => expect(mEnrollments.matricular).toHaveBeenCalledWith("p120", "e150"));
  });

  it("avisa cuando la búsqueda no encuentra registros", async () => {
    const user = userEvent.setup();

    render(<MatriculaPage />);
    await user.type(screen.getByLabelText("Buscar programa"), "zzz");

    expect(await screen.findByText("Sin resultados para «zzz».")).toBeInTheDocument();
  });

  it("muestra la carga y el error de la búsqueda, y permite reintentarla", async () => {
    let fallar!: (error: unknown) => void;
    mProgram.listarProgramas.mockReturnValueOnce(
      new Promise((_, reject) => {
        fallar = reject;
      }),
    );
    const user = userEvent.setup();

    render(<MatriculaPage />);
    expect(screen.getAllByText("Buscando…").length).toBeGreaterThan(0);
    fallar(new ApiError(503, { detail: "Servicio no disponible" }));

    expect(await screen.findByText("Servicio no disponible")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Reintentar" }));
    const filtro = screen.getByLabelText("Filtrar por programa");
    expect(await within(filtro).findByRole("option", { name: "Programa 001" })).toBeInTheDocument();
  });
});
