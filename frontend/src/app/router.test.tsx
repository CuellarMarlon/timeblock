import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { routes } from "./router";

/** Renderiza la aplicación con las rutas reales, empezando en `path`. */
function renderAt(path: string) {
  const memoryRouter = createMemoryRouter(routes, { initialEntries: [path] });
  render(<RouterProvider router={memoryRouter} />);
}

const modules = [
  { link: "Personas y perfiles", href: "/personas" },
  { link: "Programas y asignaturas", href: "/programas" },
  { link: "Matrícula", href: "/matricula" },
  { link: "Bloques de contenido", href: "/bloques" },
  { link: "Horarios", href: "/horarios" },
];

describe("navegación", () => {
  it.each(modules)("el enlace $link apunta a $href", ({ link, href }) => {
    renderAt("/");

    expect(screen.getByRole("link", { name: link })).toHaveAttribute("href", href);
  });

  it.each(modules)("al pulsar $link se abre su página", async ({ link }) => {
    const user = userEvent.setup();
    renderAt("/");

    await user.click(screen.getByRole("link", { name: link }));

    expect(screen.getByRole("heading", { level: 1, name: link })).toBeInTheDocument();
  });

  it("marca como activo el enlace de la sección actual", () => {
    renderAt("/horarios");

    expect(screen.getByRole("link", { name: "Horarios" })).toHaveAttribute("aria-current", "page");
    expect(screen.getByRole("link", { name: "Inicio" })).not.toHaveAttribute("aria-current");
  });

  it("muestra la página de no encontrado en rutas inexistentes", () => {
    renderAt("/ruta-que-no-existe");

    expect(screen.queryByRole("heading", { name: "Horarios" })).not.toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: "Navegación principal" })).toBeInTheDocument();
  });
});
