import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  actualizarPerfil,
  actualizarPersona,
  crearPersona,
  eliminarPersona,
  listarAutorizaciones,
  listarPersonas,
  type Persona,
} from "../../api/persons";
import { listarProgramas } from "../../api/program";
import { ApiError } from "../../api/errors";
import { PersonasPanel } from "./PersonasPanel";

vi.mock("../../api/persons");
vi.mock("../../api/program");

const listar = vi.mocked(listarPersonas);
const crear = vi.mocked(crearPersona);
const actualizarDatos = vi.mocked(actualizarPersona);
const actualizarElPerfil = vi.mocked(actualizarPerfil);

const coordinador: Persona = {
  id: "c1",
  nombre: "Luis García",
  perfil: { tipo: "coordinador", especialidad: "Bases de datos" },
};
const estudiante: Persona = {
  id: "e1",
  nombre: "Ana Pérez",
  perfil: { tipo: "estudiante", edad: 20, grupo: "C311", coordinadorId: "c1" },
};
const jefe: Persona = {
  id: "j1",
  nombre: "María Rodríguez",
  perfil: { tipo: "jefeDepartamento" },
};

function pagina(items: Persona[]) {
  return { items, page: 1, pageSize: 100, totalItems: items.length };
}

type User = ReturnType<typeof userEvent.setup>;

/** Espera a que cargue el listado y abre el formulario de alta. */
async function abrirNuevaPersona(user: User) {
  const boton = screen.getByRole("button", { name: "+ Nueva persona" });
  await waitFor(() => expect(boton).toBeEnabled());
  await user.click(boton);
}

async function guardar(user: User) {
  await user.click(screen.getByRole("button", { name: "Guardar persona" }));
}

describe("PersonasPanel", () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it("permite cancelar sin eliminar", async () => {
    listar.mockResolvedValue(pagina([jefe]));
    const user = userEvent.setup();
    render(<PersonasPanel />);
    await user.click(await screen.findByRole("button", { name: "Eliminar María Rodríguez" }));
    await user.click(screen.getByRole("button", { name: "Cancelar" }));
    expect(eliminarPersona).not.toHaveBeenCalled();
  });

  it("elimina una persona y recarga el listado", async () => {
    listar.mockResolvedValue(pagina([jefe]));
    vi.mocked(eliminarPersona).mockResolvedValue(undefined);
    const user = userEvent.setup();
    render(<PersonasPanel />);
    await user.click(await screen.findByRole("button", { name: "Eliminar María Rodríguez" }));
    listar.mockResolvedValue(pagina([]));
    await user.click(screen.getByRole("button", { name: "Confirmar eliminación" }));
    expect(eliminarPersona).toHaveBeenCalledWith("j1");
    expect(await screen.findByText("Persona eliminada correctamente.")).toBeInTheDocument();
    expect(await screen.findByText("Todavía no hay personas registradas.")).toBeInTheDocument();
  });

  it("muestra el conflicto cuando tiene dependencias", async () => {
    listar.mockResolvedValue(pagina([coordinador]));
    vi.mocked(eliminarPersona).mockRejectedValue(
      new ApiError(409, { detail: "La persona tiene registros relacionados." }),
    );
    const user = userEvent.setup();
    render(<PersonasPanel />);
    await user.click(await screen.findByRole("button", { name: "Eliminar Luis García" }));
    await user.click(screen.getByRole("button", { name: "Confirmar eliminación" }));
    expect(await screen.findByText("La persona tiene registros relacionados.")).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });

  it("mantiene el modal abierto mientras elimina", async () => {
    listar.mockResolvedValue(pagina([jefe]));
    vi.mocked(eliminarPersona).mockReturnValue(new Promise<void>(() => {}));
    const user = userEvent.setup();
    render(<PersonasPanel />);
    await user.click(await screen.findByRole("button", { name: "Eliminar María Rodríguez" }));
    await user.click(screen.getByRole("button", { name: "Confirmar eliminación" }));
    expect(screen.getByRole("button", { name: "Eliminando…" })).toBeDisabled();
    await user.keyboard("{Escape}");
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });

  it("muestra la carga mientras llega el listado", () => {
    listar.mockReturnValue(new Promise<never>(() => {}));
    render(<PersonasPanel />);

    expect(screen.getByText(/cargando personas/i)).toBeInTheDocument();
  });

  it("muestra el estado vacío", async () => {
    listar.mockResolvedValue(pagina([]));
    render(<PersonasPanel />);

    expect(await screen.findByText("Todavía no hay personas registradas.")).toBeInTheDocument();
  });

  it("muestra el error de la API y permite reintentar", async () => {
    const user = userEvent.setup();
    listar.mockRejectedValueOnce(new Error("fallo")).mockResolvedValueOnce(pagina([estudiante]));
    render(<PersonasPanel />);

    await user.click(await screen.findByRole("button", { name: /reintentar/i }));

    expect(await screen.findByText("Ana Pérez")).toBeInTheDocument();
    expect(listar).toHaveBeenCalledTimes(2);
  });

  it("lista las personas con su perfil y el detalle", async () => {
    listar.mockResolvedValue(pagina([estudiante, coordinador, jefe]));
    render(<PersonasPanel />);

    expect(await screen.findByText("Ana Pérez")).toBeInTheDocument();
    expect(screen.getByText("Estudiante")).toBeInTheDocument();
    expect(screen.getByText("20 años · C311 · Luis García")).toBeInTheDocument();
    expect(screen.getByText("Bases de datos")).toBeInTheDocument();
    expect(screen.getByText("Jefe de departamento")).toBeInTheDocument();
  });

  it("pagina el listado de 10 en 10", async () => {
    const user = userEvent.setup();
    const muchos: Persona[] = Array.from({ length: 12 }, (_, i) => ({
      id: `j${i + 1}`,
      nombre: `Jefe ${i + 1}`,
      perfil: { tipo: "jefeDepartamento" },
    }));
    listar.mockResolvedValue(pagina(muchos));
    render(<PersonasPanel />);

    expect(await screen.findByText("Jefe 1")).toBeInTheDocument();
    expect(screen.getByText("Página 1 de 2")).toBeInTheDocument();
    expect(screen.queryByText("Jefe 11")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Siguiente" }));

    expect(screen.getByText("Jefe 11")).toBeInTheDocument();
    expect(screen.getByText("Página 2 de 2")).toBeInTheDocument();
  });

  it("valida el nombre y el perfil sin llamar a la API", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue(pagina([coordinador]));
    render(<PersonasPanel />);

    await abrirNuevaPersona(user);
    await guardar(user);

    expect(screen.getByText("El nombre es obligatorio.")).toBeInTheDocument();
    expect(screen.getByText("Selecciona un perfil.")).toBeInTheDocument();
    expect(crear).not.toHaveBeenCalled();
  });

  it("valida los campos del perfil de estudiante", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue(pagina([coordinador]));
    render(<PersonasPanel />);

    await abrirNuevaPersona(user);
    await user.type(screen.getByLabelText("Nombre"), "Ana Pérez");
    await user.selectOptions(screen.getByLabelText("Perfil"), "estudiante");
    await user.type(screen.getByLabelText("Edad"), "0");
    await guardar(user);

    expect(
      screen.getByText("La edad debe ser un número entero mayor que cero."),
    ).toBeInTheDocument();
    expect(screen.getByText("El grupo es obligatorio.")).toBeInTheDocument();
    expect(screen.getByText("Selecciona un coordinador.")).toBeInTheDocument();
    expect(crear).not.toHaveBeenCalled();
  });

  it("valida la especialidad del coordinador", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue(pagina([]));
    render(<PersonasPanel />);

    await abrirNuevaPersona(user);
    await user.type(screen.getByLabelText("Nombre"), "Luis García");
    await user.selectOptions(screen.getByLabelText("Perfil"), "coordinador");
    await guardar(user);

    expect(screen.getByText("La especialidad es obligatoria.")).toBeInTheDocument();
    expect(crear).not.toHaveBeenCalled();
  });

  it("avisa si no hay coordinadores para asignar a un estudiante", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue(pagina([jefe]));
    render(<PersonasPanel />);

    await abrirNuevaPersona(user);
    await user.selectOptions(screen.getByLabelText("Perfil"), "estudiante");

    expect(
      screen.getByText("Primero crea un coordinador para poder asignarlo."),
    ).toBeInTheDocument();
  });

  it("crea un estudiante con el discriminador del perfil, cierra el modal y recarga", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue(pagina([coordinador]));
    crear.mockResolvedValue(estudiante);
    render(<PersonasPanel />);

    await abrirNuevaPersona(user);
    await user.type(screen.getByLabelText("Nombre"), "Ana Pérez");
    await user.selectOptions(screen.getByLabelText("Perfil"), "estudiante");
    await user.type(screen.getByLabelText("Edad"), "20");
    await user.type(screen.getByLabelText("Grupo"), "C311");
    await user.selectOptions(screen.getByLabelText("Coordinador asignado"), "c1");
    await guardar(user);

    expect(await screen.findByText("Persona creada correctamente.")).toBeInTheDocument();
    expect(crear).toHaveBeenCalledWith({
      nombre: "Ana Pérez",
      perfil: { tipo: "estudiante", edad: 20, grupo: "C311", coordinadorId: "c1" },
    });
    expect(screen.queryByLabelText("Nombre")).not.toBeInTheDocument();
    await waitFor(() => expect(listar).toHaveBeenCalledTimes(2));
  });

  it("crea un coordinador con su especialidad", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue(pagina([]));
    crear.mockResolvedValue(coordinador);
    render(<PersonasPanel />);

    await abrirNuevaPersona(user);
    await user.type(screen.getByLabelText("Nombre"), "Luis García");
    await user.selectOptions(screen.getByLabelText("Perfil"), "coordinador");
    await user.type(screen.getByLabelText("Especialidad"), "Bases de datos");
    await guardar(user);

    expect(await screen.findByText("Persona creada correctamente.")).toBeInTheDocument();
    expect(crear).toHaveBeenCalledWith({
      nombre: "Luis García",
      perfil: { tipo: "coordinador", especialidad: "Bases de datos" },
    });
  });

  it("crea un jefe de departamento solo con el tipo de perfil", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue(pagina([]));
    crear.mockResolvedValue(jefe);
    render(<PersonasPanel />);

    await abrirNuevaPersona(user);
    await user.type(screen.getByLabelText("Nombre"), "María Rodríguez");
    await user.selectOptions(screen.getByLabelText("Perfil"), "jefeDepartamento");
    await guardar(user);

    expect(await screen.findByText("Persona creada correctamente.")).toBeInTheDocument();
    expect(crear).toHaveBeenCalledWith({
      nombre: "María Rodríguez",
      perfil: { tipo: "jefeDepartamento" },
    });
  });

  it("mantiene el modal abierto cuando la API falla al crear", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue(pagina([]));
    crear.mockRejectedValue(new Error("Persona ya registrada"));
    render(<PersonasPanel />);

    await abrirNuevaPersona(user);
    await user.type(screen.getByLabelText("Nombre"), "María Rodríguez");
    await user.selectOptions(screen.getByLabelText("Perfil"), "jefeDepartamento");
    await guardar(user);

    await waitFor(() => expect(crear).toHaveBeenCalledTimes(1));
    expect(await screen.findByRole("button", { name: "Guardar persona" })).toBeEnabled();
    expect(screen.getByLabelText("Nombre")).toBeInTheDocument();
    expect(screen.queryByText("Persona creada correctamente.")).not.toBeInTheDocument();
  });

  it("edita los datos comunes con el formulario precargado y sin cambiar el tipo", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue(pagina([coordinador, estudiante]));
    actualizarDatos.mockResolvedValue({ ...estudiante, nombre: "Ana María Pérez" });
    render(<PersonasPanel />);

    await user.click(await screen.findByRole("button", { name: "Editar Ana Pérez" }));

    expect(screen.getByLabelText("Nombre")).toHaveValue("Ana Pérez");
    expect(screen.getByLabelText("Perfil")).toBeDisabled();
    expect(screen.getByLabelText("Perfil")).toHaveValue("estudiante");
    expect(screen.getByLabelText("Edad")).toHaveValue(20);

    await user.clear(screen.getByLabelText("Nombre"));
    await user.type(screen.getByLabelText("Nombre"), "Ana María Pérez");
    await guardar(user);

    expect(await screen.findByText("Persona actualizada correctamente.")).toBeInTheDocument();
    expect(actualizarDatos).toHaveBeenCalledWith("e1", { nombre: "Ana María Pérez" });
    expect(actualizarElPerfil).not.toHaveBeenCalled();
  });

  it("edita solo el perfil cuando cambian sus campos", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue(pagina([coordinador, estudiante]));
    actualizarElPerfil.mockResolvedValue({
      ...estudiante,
      perfil: { tipo: "estudiante", edad: 20, grupo: "C312", coordinadorId: "c1" },
    });
    render(<PersonasPanel />);

    await user.click(await screen.findByRole("button", { name: "Editar Ana Pérez" }));
    await user.clear(screen.getByLabelText("Grupo"));
    await user.type(screen.getByLabelText("Grupo"), "C312");
    await guardar(user);

    expect(await screen.findByText("Persona actualizada correctamente.")).toBeInTheDocument();
    expect(actualizarElPerfil).toHaveBeenCalledWith("e1", {
      tipo: "estudiante",
      edad: 20,
      grupo: "C312",
      coordinadorId: "c1",
    });
    expect(actualizarDatos).not.toHaveBeenCalled();
  });

  it("ofrece Autorizaciones solo a los coordinadores y abre su modal", async () => {
    const user = userEvent.setup();
    listar.mockResolvedValue(pagina([estudiante, coordinador, jefe]));
    vi.mocked(listarAutorizaciones).mockResolvedValue([]);
    vi.mocked(listarProgramas).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 100,
      totalItems: 0,
    });
    render(<PersonasPanel />);

    await screen.findByText("Ana Pérez");
    const botones = screen.getAllByRole("button", { name: "Autorizaciones" });
    expect(botones).toHaveLength(1);

    const filaCoordinador = screen.getByText("Luis García").closest("tr");
    expect(filaCoordinador).not.toBeNull();
    await user.click(
      within(filaCoordinador as HTMLElement).getByRole("button", { name: "Autorizaciones" }),
    );

    expect(await screen.findByText("Autorizaciones de Luis García")).toBeInTheDocument();
    expect(vi.mocked(listarAutorizaciones).mock.calls[0][0]).toBe("c1");
  });
});
