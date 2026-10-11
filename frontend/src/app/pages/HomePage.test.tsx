import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { HomePage } from "./HomePage";

describe("HomePage", () => {
  it("muestra el nombre del sistema", () => {
    render(<HomePage />);

    expect(screen.getByRole("heading", { name: "Timeblock" })).toBeInTheDocument();
  });
});
