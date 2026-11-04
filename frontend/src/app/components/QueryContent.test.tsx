import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ApiError } from "../../api/errors";
import type { PagedResult } from "../../api/types";
import { QueryContent } from "./QueryContent";

const pagina = (items: string[]): PagedResult<string> => ({
  items,
  page: 1,
  pageSize: 20,
  totalItems: items.length,
});

describe("QueryContent", () => {
  it("muestra la carga", () => {
    render(<QueryContent query={{ status: "loading" }}>{() => null}</QueryContent>);

    expect(screen.getByRole("status")).toHaveTextContent("Cargando…");
  });

  it("muestra el error con los errores por campo y permite reintentar", async () => {
    const onRetry = vi.fn();
    const error = new ApiError(400, {
      title: "La solicitud contiene datos inválidos",
      detail: "Revisa los campos indicados.",
      errors: { edad: ["La edad debe ser mayor que cero."] },
    });

    render(
      <QueryContent query={{ status: "error", error }} onRetry={onRetry}>
        {() => null}
      </QueryContent>,
    );

    const alerta = screen.getByRole("alert");
    expect(alerta).toHaveTextContent("La solicitud contiene datos inválidos");
    expect(alerta).toHaveTextContent("Revisa los campos indicados.");
    expect(alerta).toHaveTextContent("La edad debe ser mayor que cero.");

    await userEvent.click(screen.getByRole("button", { name: "Reintentar" }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it("muestra el estado vacío para una página sin elementos", () => {
    render(
      <QueryContent query={{ status: "success", data: pagina([]) }} emptyMessage="Sin personas.">
        {() => <p>contenido</p>}
      </QueryContent>,
    );

    expect(screen.getByRole("status")).toHaveTextContent("Sin personas.");
    expect(screen.queryByText("contenido")).not.toBeInTheDocument();
  });

  it("muestra el contenido cuando hay datos", () => {
    render(
      <QueryContent query={{ status: "success", data: pagina(["Ana", "Luis"]) }}>
        {(data) => <p>{data.items.join(", ")}</p>}
      </QueryContent>,
    );

    expect(screen.getByText("Ana, Luis")).toBeInTheDocument();
  });
});
