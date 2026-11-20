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

async function abrirModal(user: ReturnType<typeof userEvent.setup>) {
  const boton = await screen.findByRole("button", { name: "+ Nueva matrícula" });
  await waitFor(() => expect(boton).toBeEnabled());
  await user.click(boton);
  return screen.getByRole("dialog");
}

describe("MatriculaPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mPersons.listarPersonas.mockResolvedValue(pagina([luis, ana]));
    mProgram.listarProgramas.mockResolvedValue(pagina([ingenieria]));
    mEnrollments.listarMatriculas.mockResolvedValue(pagina([matricula]));
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
    await user.selectOptions(screen.getByLabelText("Filtrar por estudiante"), "e1");

    await waitFor(() =>
      expect(mEnrollments.listarMatriculas).toHaveBeenLastCalledWith(
        { estudianteId: "e1", programaId: undefined },
        { page: 1, pageSize: 10 },
        expect.any(AbortSignal),
      ),
    );

    await user.selectOptions(screen.getByLabelText("Filtrar por programa"), "p1");

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
    await user.selectOptions(within(dialogo).getByLabelText("Estudiante"), "e1");
    await user.selectOptions(within(dialogo).getByLabelText("Programa académico"), "p1");
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
    await user.selectOptions(within(dialogo).getByLabelText("Estudiante"), "e1");
    await user.selectOptions(within(dialogo).getByLabelText("Programa académico"), "p1");
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
    await user.selectOptions(within(dialogo).getByLabelText("Estudiante"), "e1");
    await user.selectOptions(within(dialogo).getByLabelText("Programa académico"), "p1");
    await user.click(within(dialogo).getByRole("button", { name: "Matricular" }));

    expect(await screen.findByText("La matrícula ya existe.")).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });
});
