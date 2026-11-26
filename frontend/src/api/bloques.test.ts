import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "./client";
import { cambiarEstadoBloque, desasociarBloqueDePrograma, eliminarBloque } from "./bloques";

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

it("actualiza el estado de un bloque", async () => {
  const put = vi.spyOn(apiClient, "put").mockResolvedValue({});
  try {
    await cambiarEstadoBloque("b1", false);
    expect(put).toHaveBeenCalledWith("/api/bloques/b1/estado", { activo: false });
  } finally {
    vi.restoreAllMocks();
  }
});
