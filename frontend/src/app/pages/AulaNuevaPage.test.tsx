import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { ApiError } from "../../api/errors";
import { AulaNuevaPage } from "./AulaNuevaPage";

const mocks = vi.hoisted(() => ({ crearAula: vi.fn(), obtenerSesion: vi.fn() }));

vi.mock("../../api/sesion", () => ({ obtenerSesion: mocks.obtenerSesion }));

vi.mock("../../api/aulas", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/aulas")>()),
  crearAula: mocks.crearAula,
}));

const administrador = {
  cuentaId: "c1",
  usuario: "admin",
  rol: "administrador",
  personaId: null,
  nombre: null,
};

function Listado() {
  const aviso = (useLocation().state as { aviso?: string } | null)?.aviso;
  return <p>Listado de aulas. {aviso}</p>;
}

function renderPagina() {
  return render(
    <MemoryRouter initialEntries={["/aulas/nueva"]}>
      <Routes>
        <Route path="/aulas/nueva" element={<AulaNuevaPage />} />
        <Route path="/aulas" element={<Listado />} />
      </Routes>
    </MemoryRouter>,
  );
}

async function renderConFormulario() {
  renderPagina();
  await screen.findByLabelText("Edificio");
}

async function llenar(
  user: ReturnType<typeof userEvent.setup>,
  edificio: string,
  capacidad: string,
) {
  const campoEdificio = screen.getByLabelText("Edificio");
  const campoCapacidad = screen.getByLabelText("Capacidad");
  await user.clear(campoEdificio);
  await user.clear(campoCapacidad);
  if (edificio) await user.type(campoEdificio, edificio);
  if (capacidad) await user.type(campoCapacidad, capacidad);
}

describe("AulaNuevaPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.obtenerSesion.mockResolvedValue(administrador);
  });

  it("muestra el formulario vacío y no pide ni muestra un tipo de aula", async () => {
    await renderConFormulario();

    expect(screen.getByRole("heading", { name: "Nueva aula" })).toBeInTheDocument();
    expect(screen.getByLabelText("Edificio")).toHaveValue("");
    expect(screen.getByLabelText("Capacidad")).toHaveValue(null);
    expect(screen.queryByLabelText(/tipo/i)).not.toBeInTheDocument();
  });

  it("valida los campos obligatorios sin llamar a la API", async () => {
    const user = userEvent.setup();
    await renderConFormulario();

    await user.click(screen.getByRole("button", { name: "Crear aula" }));

    expect(screen.getByText("El edificio es obligatorio.")).toBeInTheDocument();
    expect(screen.getByText("La capacidad es obligatoria.")).toBeInTheDocument();
    expect(screen.getByLabelText("Edificio")).toHaveAttribute("aria-invalid", "true");
    expect(mocks.crearAula).not.toHaveBeenCalled();
  });

  it.each([
    ["0", "La capacidad debe ser mayor que cero."],
    ["-5", "La capacidad debe ser mayor que cero."],
    ["30.5", "La capacidad debe ser un número entero."],
    ["1001", "La capacidad no puede superar 1000."],
  ])("rechaza la capacidad %s", async (capacidad, mensaje) => {
    const user = userEvent.setup();
    await renderConFormulario();

    await llenar(user, "Edificio A", capacidad);
    await user.click(screen.getByRole("button", { name: "Crear aula" }));

    expect(screen.getByText(mensaje)).toBeInTheDocument();
    expect(mocks.crearAula).not.toHaveBeenCalled();
  });

  it("rechaza un edificio demasiado largo", async () => {
    const user = userEvent.setup();
    await renderConFormulario();

    await llenar(user, "E".repeat(101), "30");
    await user.click(screen.getByRole("button", { name: "Crear aula" }));

    expect(
      screen.getByText("El edificio no puede superar los 100 caracteres."),
    ).toBeInTheDocument();
    expect(mocks.crearAula).not.toHaveBeenCalled();
  });

  it("crea el aula con los datos limpios y vuelve al listado con un aviso", async () => {
    const user = userEvent.setup();
    mocks.crearAula.mockResolvedValue({ id: "a1", edificio: "Edificio A", capacidad: 30 });
    await renderConFormulario();

    await llenar(user, "  Edificio A  ", "30");
    await user.click(screen.getByRole("button", { name: "Crear aula" }));

    expect(mocks.crearAula).toHaveBeenCalledWith({ edificio: "Edificio A", capacidad: 30 });
    expect(
      await screen.findByText("Listado de aulas. Aula creada correctamente."),
    ).toBeInTheDocument();
  });

  it("muestra los errores de validación de la API y conserva lo escrito", async () => {
    const user = userEvent.setup();
    mocks.crearAula.mockRejectedValue(
      new ApiError(400, {
        title: "La solicitud contiene datos inválidos",
        status: 400,
        errors: { capacidad: ["La capacidad debe ser mayor que cero."] },
      }),
    );
    await renderConFormulario();

    await llenar(user, "Edificio A", "30");
    await user.click(screen.getByRole("button", { name: "Crear aula" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "La capacidad debe ser mayor que cero.",
    );
    expect(screen.getByLabelText("Edificio")).toHaveValue("Edificio A");
    expect(screen.getByLabelText("Capacidad")).toHaveValue(30);
    expect(screen.getByRole("button", { name: "Crear aula" })).toBeEnabled();
  });

  it("bloquea el botón mientras se guarda", async () => {
    const user = userEvent.setup();
    mocks.crearAula.mockReturnValue(new Promise(() => {}));
    await renderConFormulario();

    await llenar(user, "Edificio A", "30");
    await user.click(screen.getByRole("button", { name: "Crear aula" }));

    expect(await screen.findByRole("button", { name: "Guardando…" })).toBeDisabled();
  });

  it.each(["coordinador", "estudiante", "jefeDepartamento"])(
    "no deja crear aulas al rol %s",
    async (rol) => {
      mocks.obtenerSesion.mockResolvedValue({ ...administrador, rol });
      renderPagina();

      expect(await screen.findByRole("alert")).toHaveTextContent("La operación no está permitida.");
      expect(screen.queryByLabelText("Edificio")).not.toBeInTheDocument();
    },
  );

  it("pide iniciar sesión cuando no hay sesión", async () => {
    mocks.obtenerSesion.mockRejectedValue(new ApiError(401));
    renderPagina();

    expect(await screen.findByRole("alert")).toHaveTextContent("Inicia sesión de nuevo.");
    expect(screen.queryByLabelText("Edificio")).not.toBeInTheDocument();
  });

  it("muestra que se están comprobando los permisos antes de mostrar el formulario", async () => {
    mocks.obtenerSesion.mockReturnValue(new Promise(() => {}));
    renderPagina();

    expect(await screen.findByText("Comprobando permisos…")).toBeInTheDocument();
    expect(screen.queryByLabelText("Edificio")).not.toBeInTheDocument();
  });

  it("deja crear mientras el servidor no tenga el endpoint de sesión", async () => {
    mocks.obtenerSesion.mockRejectedValue(new ApiError(404));
    await renderConFormulario();

    expect(screen.getByRole("button", { name: "Crear aula" })).toBeEnabled();
  });

  it("muestra el mensaje y conserva lo escrito cuando la API responde 403 al crear", async () => {
    const user = userEvent.setup();
    mocks.crearAula.mockRejectedValue(new ApiError(403));
    await renderConFormulario();

    await llenar(user, "Edificio A", "30");
    await user.click(screen.getByRole("button", { name: "Crear aula" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("La operación no está permitida.");
    expect(screen.getByLabelText("Edificio")).toHaveValue("Edificio A");
  });
});
