import { describe, expect, it } from "vitest";
import { ApiError, describeError } from "./errors";

describe("describeError", () => {
  it("usa el título y el detalle del Problem Details", () => {
    const error = new ApiError(409, {
      title: "Conflicto",
      detail: "Ya existe una persona con ese carné.",
    });

    expect(describeError(error)).toEqual({
      title: "Conflicto",
      detail: "Ya existe una persona con ese carné.",
      fieldErrors: {},
    });
  });

  it("usa un título por código si la API no envía uno", () => {
    expect(describeError(new ApiError(403)).title).toBe("La operación no está permitida.");
    expect(describeError(new ApiError(503)).title).toBe(
      "Ocurrió un error inesperado en el servidor.",
    );
  });

  it("describe los fallos de conexión", () => {
    expect(describeError(new ApiError(0)).title).toMatch(/No se pudo conectar/);
  });

  it("no repite el detalle cuando coincide con el título", () => {
    const error = new ApiError(400, { title: "Datos inválidos", detail: "Datos inválidos" });

    expect(describeError(error).detail).toBeUndefined();
  });

  it("no expone el mensaje de errores ajenos a la API", () => {
    expect(describeError(new Error("detalle interno"))).toEqual({
      title: "Ocurrió un error inesperado.",
      fieldErrors: {},
    });
  });
});
