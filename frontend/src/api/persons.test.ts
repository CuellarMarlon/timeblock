import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "./client";
import { eliminarAutorizacion } from "./persons";

describe("eliminarAutorizacion", () => {
  afterEach(() => vi.restoreAllMocks());

  it("envía coordinador, programa y fecha al endpoint DELETE", async () => {
    const borrar = vi.spyOn(apiClient, "delete").mockResolvedValue(undefined);
    await eliminarAutorizacion("c1", "p1", "2026-09-07");
    expect(borrar).toHaveBeenCalledWith("/api/coordinadores/c1/autorizaciones/p1/2026-09-07");
  });
});
