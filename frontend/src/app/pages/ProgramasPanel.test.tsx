import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ApiError } from "../../api/errors";
import * as api from "../../api/program";
import { ProgramasPanel } from "./ProgramasPanel";

vi.mock("../../api/program");

const mocked = vi.mocked(api);

function pagina(items: api.Programa[]) {
  return { items, page: 1, pageSize: 10, totalItems: items.length };
}

describe("ProgramasPanel", () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it("muestra el estado de carga", () => {
    mocked.listarProgramas.mockReturnValue(new Promise(() => {}));

    render(<ProgramasPanel />);

    expect(screen.getByText("Cargando programas…")).toBeInTheDocument();
  });

  it("muestra el estado vacío", async () => {
    mocked.listarProgramas.mockResolvedValue(pagina([]));

    render(<ProgramasPanel />);

    expect(await screen.findByText("Todavía no hay programas registrados.")).toBeInTheDocument();
  });

  it("muestra el error de la API y permite reintentar", async () => {
    mocked.listarProgramas.mockRejectedValueOnce(
      new ApiError(500, { detail: "Fallo del servidor" }),
    );
    mocked.listarProgramas.mockResolvedValueOnce(
      pagina([{ id: "1", nombre: "Ingeniería", planEstudio: "2026" }]),
    );
    const user = userEvent.setup();

    render(<ProgramasPanel />);

    expect(await screen.findByRole("alert")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Reintentar" }));
    expect(await screen.findByText("Ingeniería")).toBeInTheDocument();
  });

  it("muestra los programas recibidos", async () => {
    mocked.listarProgramas.mockResolvedValue(
      pagina([{ id: "1", nombre: "Ingeniería", planEstudio: "Plan 2026" }]),
    );

    render(<ProgramasPanel />);

    expect(await screen.findByText("Ingeniería")).toBeInTheDocument();
    expect(screen.getByText("Plan 2026")).toBeInTheDocument();
  });

  it("valida los campos obligatorios sin llamar a la API", async () => {
    mocked.listarProgramas.mockResolvedValue(pagina([]));
    const user = userEvent.setup();

    render(<ProgramasPanel />);
    await user.click(await screen.findByRole("button", { name: "+ Nuevo programa" }));
    await user.click(screen.getByRole("button", { name: "Guardar programa" }));

    expect(screen.getByText("El nombre es obligatorio.")).toBeInTheDocument();
    expect(screen.getByText("El plan de estudio es obligatorio.")).toBeInTheDocument();
    expect(mocked.crearPrograma).not.toHaveBeenCalled();
  });

  it("crea un programa, cierra el modal y recarga el listado", async () => {
    mocked.listarProgramas.mockResolvedValue(pagina([]));
    mocked.crearPrograma.mockResolvedValue({ id: "2", nombre: "Datos", planEstudio: "2027" });
    const user = userEvent.setup();

    render(<ProgramasPanel />);
    await user.click(await screen.findByRole("button", { name: "+ Nuevo programa" }));
    await user.type(screen.getByLabelText("Nombre"), "Datos");
    await user.type(screen.getByLabelText("Plan de estudio"), "2027");
    await user.click(screen.getByRole("button", { name: "Guardar programa" }));

    await waitFor(() =>
      expect(mocked.crearPrograma).toHaveBeenCalledWith({ nombre: "Datos", planEstudio: "2027" }),
    );
    expect(await screen.findByText("Programa creado correctamente.")).toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(mocked.listarProgramas).toHaveBeenCalledTimes(2);
  });

  it("muestra el error de la API al crear y mantiene el modal abierto", async () => {
    mocked.listarProgramas.mockResolvedValue(pagina([]));
    mocked.crearPrograma.mockRejectedValue(
      new ApiError(400, { detail: "Datos inválidos", errors: { nombre: ["Nombre duplicado."] } }),
    );
    const user = userEvent.setup();

    render(<ProgramasPanel />);
    await user.click(await screen.findByRole("button", { name: "+ Nuevo programa" }));
    await user.type(screen.getByLabelText("Nombre"), "X");
    await user.type(screen.getByLabelText("Plan de estudio"), "Y");
    await user.click(screen.getByRole("button", { name: "Guardar programa" }));

    expect(await screen.findByText("Nombre duplicado.")).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });

  it("edita un programa con los datos precargados", async () => {
    mocked.listarProgramas.mockResolvedValue(
      pagina([{ id: "1", nombre: "Ingeniería", planEstudio: "2026" }]),
    );
    mocked.actualizarPrograma.mockResolvedValue({ id: "1", nombre: "Ing. 2", planEstudio: "2026" });
    const user = userEvent.setup();

    render(<ProgramasPanel />);
    await user.click(await screen.findByRole("button", { name: "Editar Ingeniería" }));

    const nombre = screen.getByLabelText("Nombre");
    expect(nombre).toHaveValue("Ingeniería");
    await user.clear(nombre);
    await user.type(nombre, "Ing. 2");
    await user.click(screen.getByRole("button", { name: "Guardar programa" }));

    await waitFor(() =>
      expect(mocked.actualizarPrograma).toHaveBeenCalledWith("1", {
        nombre: "Ing. 2",
        planEstudio: "2026",
      }),
    );
    expect(await screen.findByText("Programa actualizado correctamente.")).toBeInTheDocument();
  });
});
