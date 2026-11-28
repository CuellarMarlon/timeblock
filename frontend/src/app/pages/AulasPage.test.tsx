import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { ApiError } from "../../api/errors";
import { AulasPage } from "./AulasPage";

const mocks = vi.hoisted(() => ({ listarAulas: vi.fn() }));

vi.mock("../../api/aulas", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/aulas")>()),
  listarAulas: mocks.listarAulas,
}));

const aula = { id: "a1", edificio: "Edificio A", capacidad: 30 };

function pagina(items: unknown[], extra: Record<string, unknown> = {}) {
  return { items, page: 1, pageSize: 20, totalItems: items.length, ...extra };
}

function renderPagina(estado?: unknown) {
  return render(
    <MemoryRouter initialEntries={[{ pathname: "/aulas", state: estado }]}>
      <AulasPage />
    </MemoryRouter>,
  );
}

describe("AulasPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it("muestra la carga y después las aulas con su edificio y capacidad", async () => {
    mocks.listarAulas.mockResolvedValue(pagina([aula]));
    renderPagina();

    expect(screen.getByText("Cargando aulas…")).toBeInTheDocument();

    const fila = await screen.findByRole("row", { name: /Edificio A/ });
    expect(fila).toHaveTextContent("30");
    expect(
      screen.getByRole("link", { name: "Editar aula de Edificio A con capacidad 30" }),
    ).toHaveAttribute("href", "/aulas/a1/editar");
    expect(screen.getByRole("link", { name: "+ Nueva aula" })).toHaveAttribute(
      "href",
      "/aulas/nueva",
    );
  });

  it("muestra el estado vacío cuando no hay aulas", async () => {
    mocks.listarAulas.mockResolvedValue(pagina([]));
    renderPagina();

    expect(await screen.findByText("Todavía no hay aulas registradas.")).toBeInTheDocument();
  });

  it("muestra el error de la API y permite reintentar", async () => {
    const user = userEvent.setup();
    mocks.listarAulas
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce(pagina([aula]));
    renderPagina();

    expect(await screen.findByRole("alert")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    expect(await screen.findByRole("row", { name: /Edificio A/ })).toBeInTheDocument();
  });

  it("filtra por edificio volviendo a la primera página", async () => {
    const user = userEvent.setup();
    mocks.listarAulas.mockResolvedValue(pagina([aula]));
    renderPagina();
    await screen.findByRole("row", { name: /Edificio A/ });

    await user.type(screen.getByLabelText("Filtrar por edificio"), "  Edificio B ");
    await user.click(screen.getByRole("button", { name: "Filtrar" }));

    const [filtros, paginacion] = mocks.listarAulas.mock.lastCall ?? [];
    expect(filtros).toEqual({ edificio: "Edificio B" });
    expect(paginacion).toEqual({ page: 1, pageSize: 20 });
  });

  it("avisa cuando no hay aulas que coincidan con el filtro", async () => {
    const user = userEvent.setup();
    mocks.listarAulas.mockResolvedValueOnce(pagina([aula])).mockResolvedValue(pagina([]));
    renderPagina();
    await screen.findByRole("row", { name: /Edificio A/ });

    await user.type(screen.getByLabelText("Filtrar por edificio"), "Z");
    await user.click(screen.getByRole("button", { name: "Filtrar" }));

    expect(
      await screen.findByText("No hay aulas que coincidan con el filtro."),
    ).toBeInTheDocument();
  });

  it("cambia de página conservando el filtro", async () => {
    const user = userEvent.setup();
    mocks.listarAulas.mockResolvedValue(pagina([aula], { totalItems: 45 }));
    renderPagina();
    await screen.findByRole("row", { name: /Edificio A/ });

    await user.click(screen.getByRole("button", { name: "Siguiente" }));

    const [, paginacion] = mocks.listarAulas.mock.lastCall ?? [];
    expect(paginacion).toEqual({ page: 2, pageSize: 20 });
  });

  it("muestra el aviso que llega después de crear o editar un aula", async () => {
    mocks.listarAulas.mockResolvedValue(pagina([aula]));
    renderPagina({ aviso: "Aula creada correctamente." });

    expect(await screen.findByText("Aula creada correctamente.")).toBeInTheDocument();
  });
});
