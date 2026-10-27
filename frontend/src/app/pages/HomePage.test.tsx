import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { HomePage } from "./HomePage";

describe("HomePage", () => {
  it("muestra el mensaje de bienvenida", () => {
    render(<HomePage />);

    expect(screen.getByRole("heading", { name: "Bienvenido a Timeblock" })).toBeInTheDocument();
  });
});
