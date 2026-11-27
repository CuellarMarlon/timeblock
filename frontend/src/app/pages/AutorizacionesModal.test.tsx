import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  actualizarAutorizacion,
  crearAutorizacion,
  eliminarAutorizacion,
  listarAutorizaciones,
  type Autorizacion,
  type Persona,
} from "../../api/persons";
import { listarProgramas } from "../../api/program";
import { ApiError } from "../../api/errors";
import { AutorizacionesModal } from "./AutorizacionesModal";

vi.mock("../../api/persons");
vi.mock("../../api/program");

const listar = vi.mocked(listarAutorizaciones);
const crear = vi.mocked(crearAutorizacion);
const actualizar = vi.mocked(actualizarAutorizacion);
const eliminar = vi.mocked(eliminarAutorizacion);

const coordinador: Persona = {
  id: "c1",
  nombre: "Luis García",
  perfil: { tipo: "coordinador", especialidad: "Bases de datos" },
};

const autorizacion: Autorizacion = {
  coordinadorId: "c1",
  programaId: "p1",
  fechaInicio: "2026-09-07",
  fechaFin: null,
  puedeGenerar: true,
  puedeValidar: false,
};

const NUEVA = "+ Nueva autorización";

function cambiarFecha(etiqueta: string, valor: string) {
  fireEvent.change(screen.getByLabelText(etiqueta), { target: { value: valor } });
}

async function guardar(user: ReturnType<typeof userEvent.setup>) {
  await user.click(screen.getByRole("button", { name: "Guardar autorización" }));
}

describe("AutorizacionesModal", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.mocked(listarProgramas).mockResolvedValue({
      items: [
        { id: "p1", nombre: "Ingeniería Informática", planEstudio: "Plan 2024" },
        { id: "p2", nombre: "Matemática", planEstudio: "Plan 2024" },
      ],
      page: 1,
      pageSize: 100,
      totalItems: 2,
    });
  });

  function renderModal() {
    return render(<AutorizacionesModal coordinador={coordinador} onClose={vi.fn()} />);
  }

  it("muestra la carga mientras llegan las autorizaciones", () => {
    listar.mockReturnValue(new Promise<never>(() => {}));
    renderModal();

    expect(screen.getByText(/cargando autorizaciones/i)).toBeInTheDocument();
  });

  it("muestra el estado vacío", async () => {
    listar.mockResolvedValue([]);
    renderModal();

    expect(
      await screen.findByText("Este coordinador todavía no tiene autorizaciones."),
    ).toBeInTheDocument();
  });

  it("muestra el error de la API y permite reintentar", async () => {
    const user = userEvent.setup();
    listar.mockRejectedValueOnce(new Error("fallo")).mockResolvedValueOnce([autorizacion]);
    renderModal();

    await user.click(await screen.findByRole("button", { name: /reintentar/i }));

    expect(await screen.findByText("Ingeniería Informática")).toBeInTheDocument();
    expect(listar).toHaveBeenCalledTimes(2);
  });

  it("lista las autorizaciones con el nombre del programa y sus permisos", async () => {
    listar.mockResolvedValue([autorizacion]);
    renderModal();

    expect(await screen.findByText("Ingeniería Informática")).toBeInTheDocument();
    expect(screen.getByText("2026-09-07")).toBeInTheDocument();
    expect(screen.getByText("—")).toBeInTheDocument();
    expect(screen.getByText("Sí")).toBeInTheDocument();
    expect(screen.getByText("No")).toBeInTheDocument();
    expect(listar.mock.calls[0][0]).toBe("c1");
  });

  it("valida el programa y la fecha de inicio sin llamar a la API", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue([]);
    renderModal();

    await user.click(screen.getByRole("button", { name: NUEVA }));
    await screen.findByLabelText("Programa académico");
    await guardar(user);

    expect(screen.getByText("Selecciona un programa.")).toBeInTheDocument();
    expect(screen.getByText("La fecha de inicio es obligatoria.")).toBeInTheDocument();
    expect(crear).not.toHaveBeenCalled();
  });

  it("rechaza una fecha de fin anterior a la de inicio", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue([]);
    renderModal();

    await user.click(screen.getByRole("button", { name: NUEVA }));
    await user.selectOptions(await screen.findByLabelText("Programa académico"), "p1");
    cambiarFecha("Fecha de inicio", "2026-10-12");
    cambiarFecha("Fecha de fin (opcional)", "2026-10-01");
    await guardar(user);

    expect(
      screen.getByText("La fecha de fin no puede ser anterior a la de inicio."),
    ).toBeInTheDocument();
    expect(crear).not.toHaveBeenCalled();
  });

  it("crea una autorización, vuelve al listado y recarga", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue([]);
    crear.mockResolvedValue({ ...autorizacion, fechaInicio: "2026-10-12" });
    renderModal();

    await user.click(screen.getByRole("button", { name: NUEVA }));
    await user.selectOptions(await screen.findByLabelText("Programa académico"), "p1");
    cambiarFecha("Fecha de inicio", "2026-10-12");
    await user.click(screen.getByLabelText("Puede generar horarios"));
    await guardar(user);

    expect(await screen.findByText("Autorización creada correctamente.")).toBeInTheDocument();
    expect(crear).toHaveBeenCalledWith("c1", {
      programaId: "p1",
      fechaInicio: "2026-10-12",
      fechaFin: null,
      puedeGenerar: true,
      puedeValidar: false,
    });
    await waitFor(() => expect(listar).toHaveBeenCalledTimes(2));
  });

  it("mantiene el formulario cuando la API falla al crear", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue([]);
    crear.mockRejectedValue(new Error("Conflicto"));
    renderModal();

    await user.click(screen.getByRole("button", { name: NUEVA }));
    await user.selectOptions(await screen.findByLabelText("Programa académico"), "p1");
    cambiarFecha("Fecha de inicio", "2026-10-12");
    await guardar(user);

    await waitFor(() => expect(crear).toHaveBeenCalledTimes(1));
    expect(await screen.findByRole("button", { name: "Guardar autorización" })).toBeEnabled();
    expect(screen.queryByText("Autorización creada correctamente.")).not.toBeInTheDocument();
  });

  it("actualiza solo el fin de vigencia y los permisos", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue([autorizacion]);
    actualizar.mockResolvedValue({
      ...autorizacion,
      fechaFin: "2027-01-31",
      puedeValidar: true,
    });
    renderModal();

    await user.click(
      await screen.findByRole("button", { name: "Editar autorización de Ingeniería Informática" }),
    );

    expect(screen.getByLabelText("Programa académico")).toBeDisabled();
    expect(screen.getByLabelText("Fecha de inicio")).toBeDisabled();
    expect(screen.getByLabelText("Fecha de inicio")).toHaveValue("2026-09-07");

    cambiarFecha("Fecha de fin (opcional)", "2027-01-31");
    await user.click(screen.getByLabelText("Puede validar horarios"));
    await guardar(user);

    expect(await screen.findByText("Autorización actualizada correctamente.")).toBeInTheDocument();
    expect(actualizar).toHaveBeenCalledWith("c1", "p1", "2026-09-07", {
      fechaFin: "2027-01-31",
      puedeGenerar: true,
      puedeValidar: true,
    });
    expect(crear).not.toHaveBeenCalled();
  });

  it("cancelar el formulario vuelve al listado sin llamar a la API", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue([]);
    renderModal();

    await user.click(screen.getByRole("button", { name: NUEVA }));
    await screen.findByLabelText("Programa académico");
    await user.click(screen.getByRole("button", { name: "Cancelar" }));

    expect(await screen.findByRole("button", { name: NUEVA })).toBeInTheDocument();
    expect(crear).not.toHaveBeenCalled();
  });

  it("permite cancelar la eliminación sin llamar a la API", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue([autorizacion]);
    renderModal();
    await user.click(
      await screen.findByRole("button", {
        name: "Eliminar autorización de Ingeniería Informática desde 2026-09-07",
      }),
    );
    const dialogo = screen.getByRole("dialog", { name: "Confirmar eliminación" });
    expect(within(dialogo).getByText("2026-09-07")).toBeInTheDocument();
    expect(within(dialogo).getByText("Luis García")).toBeInTheDocument();
    await user.click(within(dialogo).getByRole("button", { name: "Cancelar" }));
    expect(eliminar).not.toHaveBeenCalled();
    expect(await screen.findByText("Ingeniería Informática")).toBeInTheDocument();
  });

  it("elimina la autorización elegida y recarga el listado", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue([autorizacion]);
    eliminar.mockResolvedValue(undefined);
    renderModal();
    await user.click(
      await screen.findByRole("button", {
        name: "Eliminar autorización de Ingeniería Informática desde 2026-09-07",
      }),
    );
    listar.mockResolvedValue([]);
    await user.click(screen.getByRole("button", { name: "Confirmar eliminación" }));
    expect(eliminar).toHaveBeenCalledWith("c1", "p1", "2026-09-07");
    expect(await screen.findByText("Autorización eliminada correctamente.")).toBeInTheDocument();
    expect(
      await screen.findByText("Este coordinador todavía no tiene autorizaciones."),
    ).toBeInTheDocument();
  });

  it("muestra el conflicto y conserva la autorización", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue([autorizacion]);
    eliminar.mockRejectedValue(
      new ApiError(409, { detail: "Un horario utiliza esta autorización." }),
    );
    renderModal();
    await user.click(
      await screen.findByRole("button", {
        name: "Eliminar autorización de Ingeniería Informática desde 2026-09-07",
      }),
    );
    await user.click(screen.getByRole("button", { name: "Confirmar eliminación" }));
    expect(await screen.findByText("Un horario utiliza esta autorización.")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Cancelar" }));
    expect(await screen.findByText("2026-09-07")).toBeInTheDocument();
  });

  it("mantiene la confirmación abierta durante la eliminación", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue([autorizacion]);
    eliminar.mockReturnValue(new Promise<void>(() => {}));
    const cerrar = vi.fn();
    render(<AutorizacionesModal coordinador={coordinador} onClose={cerrar} />);
    await user.click(
      await screen.findByRole("button", {
        name: "Eliminar autorización de Ingeniería Informática desde 2026-09-07",
      }),
    );
    await user.click(screen.getByRole("button", { name: "Confirmar eliminación" }));
    expect(screen.getByRole("button", { name: "Eliminando…" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Cancelar" })).toBeDisabled();
    await user.keyboard("{Escape}");
    expect(screen.getByRole("dialog", { name: "Confirmar eliminación" })).toBeInTheDocument();
    expect(cerrar).not.toHaveBeenCalled();
    expect(eliminar).toHaveBeenCalledTimes(1);
  });

  it("muestra el conflicto al actualizar vigencia y mantiene el formulario", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue([autorizacion]);
    actualizar.mockRejectedValue(
      new ApiError(409, { detail: "El cambio invalidaría horarios existentes." }),
    );
    renderModal();
    await user.click(
      await screen.findByRole("button", { name: "Editar autorización de Ingeniería Informática" }),
    );
    cambiarFecha("Fecha de fin (opcional)", "2026-10-01");
    await guardar(user);
    expect(
      await screen.findByText("El cambio invalidaría horarios existentes."),
    ).toBeInTheDocument();
    expect(screen.getByLabelText("Fecha de fin (opcional)")).toHaveValue("2026-10-01");
  });
});
