import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppLayout } from "./AppLayout";

/**
 * Renderiza el layout con una página hija de prueba, dentro de un
 * router en memoria (no usa el navegador real).
 */
function renderLayout() {
  render(
    <MemoryRouter initialEntries={["/"]}>
      <Routes>
        <Route element={<AppLayout />}>
          <Route index element={<p>Contenido de prueba</p>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  );
}

describe("AppLayout", () => {
  it("muestra el nombre y la descripción del sistema", () => {
    renderLayout();

    expect(screen.getByText("Timeblock")).toBeInTheDocument();
    expect(screen.getByText("Sistema para la gestión de horarios docentes.")).toBeInTheDocument();
  });

  it("muestra la navegación principal con el enlace de inicio", () => {
    renderLayout();

    const nav = screen.getByRole("navigation", { name: "Navegación principal" });
    expect(nav).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Inicio" })).toHaveAttribute("href", "/");
  });

  it("muestra la página hija dentro del área de contenido", () => {
    renderLayout();

    const main = screen.getByRole("main");
    expect(main).toHaveTextContent("Contenido de prueba");
  });
});
