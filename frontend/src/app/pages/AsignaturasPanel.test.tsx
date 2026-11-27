import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import * as api from "../../api/program";
import { ApiError } from "../../api/errors";
import { AsignaturasPanel } from "./AsignaturasPanel";

vi.mock("../../api/program");

const mocked = vi.mocked(api);

function pagina<T>(items: T[]) {
  return { items, page: 1, pageSize: 10, totalItems: items.length };
}

const programa = { id: "p1", nombre: "Ingeniería", planEstudio: "2026" };
const asignatura = { id: "a1", nombre: "Bases de Datos" };

describe("AsignaturasPanel", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocked.listarProgramas.mockResolvedValue(pagina([programa]));
    mocked.listarAsignaturas.mockResolvedValue(pagina([asignatura]));
    mocked.listarAsignaturasDePrograma.mockResolvedValue(pagina([]));
  });

  it("muestra las asignaturas recibidas", async () => {
    render(<AsignaturasPanel />);

    expect(await screen.findByRole("cell", { name: "Bases de Datos" })).toBeInTheDocument();
  });

  it("valida el nombre al crear una asignatura", async () => {
    const user = userEvent.setup();

    render(<AsignaturasPanel />);
    await user.click(await screen.findByRole("button", { name: "+ Nueva asignatura" }));
    await user.click(screen.getByRole("button", { name: "Guardar asignatura" }));

    expect(screen.getByText("El nombre es obligatorio.")).toBeInTheDocument();
    expect(mocked.crearAsignatura).not.toHaveBeenCalled();
  });

  it("crea una asignatura y muestra el aviso de éxito", async () => {
    mocked.crearAsignatura.mockResolvedValue({ id: "a2", nombre: "Álgebra" });
    const user = userEvent.setup();

    render(<AsignaturasPanel />);
    await user.click(await screen.findByRole("button", { name: "+ Nueva asignatura" }));
    await user.type(screen.getByLabelText("Nombre"), "Álgebra");
    await user.click(screen.getByRole("button", { name: "Guardar asignatura" }));

    await waitFor(() => expect(mocked.crearAsignatura).toHaveBeenCalledWith({ nombre: "Álgebra" }));
    expect(await screen.findByText("Asignatura creada correctamente.")).toBeInTheDocument();
  });

  it("valida la selección al asociar", async () => {
    const user = userEvent.setup();

    render(<AsignaturasPanel />);
    await user.click(await screen.findByRole("button", { name: "Asociar" }));

    expect(screen.getByText("Selecciona un programa.")).toBeInTheDocument();
    expect(screen.getByText("Selecciona una asignatura.")).toBeInTheDocument();
    expect(mocked.asociarAsignatura).not.toHaveBeenCalled();
  });

  it("asocia una asignatura a un programa", async () => {
    mocked.asociarAsignatura.mockResolvedValue(undefined);
    const user = userEvent.setup();

    render(<AsignaturasPanel />);
    await user.selectOptions(await screen.findByLabelText("Programa académico"), "p1");
    await user.selectOptions(screen.getByLabelText("Asignatura"), "a1");
    await user.click(screen.getByRole("button", { name: "Asociar" }));

    await waitFor(() => expect(mocked.asociarAsignatura).toHaveBeenCalledWith("p1", "a1"));
    expect(await screen.findByText("Asignatura asociada correctamente.")).toBeInTheDocument();
  });

  it("permite cancelar la eliminación", async () => {
    const user = userEvent.setup();
    render(<AsignaturasPanel />);
    await user.click(await screen.findByRole("button", { name: "Eliminar Bases de Datos" }));
    await user.click(screen.getByRole("button", { name: "Cancelar" }));
    expect(mocked.eliminarAsignatura).not.toHaveBeenCalled();
  });

  it("elimina una asignatura y recarga el listado", async () => {
    mocked.eliminarAsignatura.mockResolvedValue(undefined);
    const user = userEvent.setup();
    render(<AsignaturasPanel />);
    await user.click(await screen.findByRole("button", { name: "Eliminar Bases de Datos" }));
    mocked.listarAsignaturas.mockResolvedValue(pagina([]));
    await user.click(screen.getByRole("button", { name: "Confirmar eliminación" }));
    expect(mocked.eliminarAsignatura).toHaveBeenCalledWith("a1");
    expect(await screen.findByText("Asignatura eliminada correctamente.")).toBeInTheDocument();
    expect(await screen.findByText("Todavía no hay asignaturas registradas.")).toBeInTheDocument();
  });

  it("muestra el conflicto si tiene dependencias", async () => {
    mocked.eliminarAsignatura.mockRejectedValue(
      new ApiError(409, { detail: "La asignatura tiene registros relacionados." }),
    );
    const user = userEvent.setup();
    render(<AsignaturasPanel />);
    await user.click(await screen.findByRole("button", { name: "Eliminar Bases de Datos" }));
    await user.click(screen.getByRole("button", { name: "Confirmar eliminación" }));
    expect(
      await screen.findByText("La asignatura tiene registros relacionados."),
    ).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });

  it("mantiene el modal abierto durante la eliminación", async () => {
    mocked.eliminarAsignatura.mockReturnValue(new Promise<void>(() => {}));
    const user = userEvent.setup();
    render(<AsignaturasPanel />);
    await user.click(await screen.findByRole("button", { name: "Eliminar Bases de Datos" }));
    await user.click(screen.getByRole("button", { name: "Confirmar eliminación" }));
    expect(screen.getByRole("button", { name: "Eliminando…" })).toBeDisabled();
    await user.keyboard("{Escape}");
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });
});
