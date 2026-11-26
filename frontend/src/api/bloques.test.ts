import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "./client";
import { desasociarBloqueDePrograma, eliminarBloque } from "./bloques";

describe("desasociarBloqueDePrograma", () => {
  afterEach(() => vi.restoreAllMocks());

  it("envía la pareja al endpoint DELETE", async () => {
    const borrar = vi.spyOn(apiClient, "delete").mockResolvedValue(undefined);
    await desasociarBloqueDePrograma("b1", "p1");
    expect(borrar).toHaveBeenCalledWith("/api/bloques/b1/programas/p1");
  });
});

it("elimina un bloque por su identificador", async () => {
  const borrar = vi.spyOn(apiClient, "delete").mockResolvedValue(undefined);
  try {
    await eliminarBloque("b1");
    expect(borrar).toHaveBeenCalledWith("/api/bloques/b1");
  } finally {
    vi.restoreAllMocks();
  }
});
