import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ApiError } from "../../api/errors";
import * as api from "../../api/program";
import { AsociarAsignatura } from "./AsociarAsignatura";

vi.mock("../../api/program");
const mocked = vi.mocked(api);
const programa = { id: "p1", nombre: "Ingeniería", planEstudio: "2026" };
const asignatura = { id: "a1", nombre: "Bases de Datos" };
const pagina = <T,>(items: T[]) => ({ items, page: 1, pageSize: 100, totalItems: items.length });

describe("Desvincular asignatura", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocked.listarProgramas.mockResolvedValue(pagina([programa]));
    mocked.listarAsignaturas.mockResolvedValue(pagina([asignatura]));
    mocked.listarAsignaturasDePrograma.mockResolvedValue(pagina([asignatura]));
  });

  async function abrir(user: ReturnType<typeof userEvent.setup>) {
    render(<AsociarAsignatura />);
    await user.selectOptions(await screen.findByLabelText("Programa académico"), "p1");
    await user.click(
      await screen.findByRole("button", { name: "Desvincular Bases de Datos de Ingeniería" }),
    );
    return screen.getByRole("dialog", { name: "Confirmar desvinculación" });
  }

  it("identifica la relación y cancelar no envía la petición", async () => {
    const user = userEvent.setup();
    const dialogo = await abrir(user);
    expect(within(dialogo).getByText("Bases de Datos")).toBeInTheDocument();
    expect(within(dialogo).getByText("Ingeniería")).toBeInTheDocument();
    await user.click(within(dialogo).getByRole("button", { name: "Cancelar" }));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(mocked.desasociarAsignatura).not.toHaveBeenCalled();
  });

  it("desvincula la pareja y recarga sus asociaciones", async () => {
    mocked.desasociarAsignatura.mockResolvedValue(undefined);
    const user = userEvent.setup();
    const dialogo = await abrir(user);
    mocked.listarAsignaturasDePrograma.mockResolvedValue(pagina([]));
    await user.click(within(dialogo).getByRole("button", { name: "Confirmar desvinculación" }));
    await waitFor(() => expect(mocked.desasociarAsignatura).toHaveBeenCalledWith("p1", "a1"));
    expect(await screen.findByText("Asignatura desvinculada correctamente.")).toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(
      await screen.findByText("Este programa todavía no tiene asignaturas asociadas."),
    ).toBeInTheDocument();
  });

  it("muestra el conflicto y conserva la asociación", async () => {
    mocked.desasociarAsignatura.mockRejectedValue(
      new ApiError(409, { detail: "La asignatura tiene bloques asociados." }),
    );
    const user = userEvent.setup();
    const dialogo = await abrir(user);
    await user.click(within(dialogo).getByRole("button", { name: "Confirmar desvinculación" }));
    expect(
      await within(dialogo).findByText("La asignatura tiene bloques asociados."),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Desvincular Bases de Datos de Ingeniería" }),
    ).toBeInTheDocument();
  });

  it("impide cerrar o repetir mientras se procesa la petición", async () => {
    mocked.desasociarAsignatura.mockReturnValue(new Promise<void>(() => {}));
    const user = userEvent.setup();
    const dialogo = await abrir(user);
    await user.click(within(dialogo).getByRole("button", { name: "Confirmar desvinculación" }));
    expect(within(dialogo).getByRole("button", { name: "Desvinculando…" })).toBeDisabled();
    expect(within(dialogo).getByRole("button", { name: "Cancelar" })).toBeDisabled();
    await user.keyboard("{Escape}");
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(mocked.desasociarAsignatura).toHaveBeenCalledTimes(1);
  });
});
