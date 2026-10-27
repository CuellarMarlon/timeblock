import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";

// Desmonta los componentes renderizados después de cada prueba
// para que unas pruebas no afecten a otras.
afterEach(() => {
  cleanup();
});
