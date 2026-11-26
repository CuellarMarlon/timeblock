import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "./client";
import { desasociarAsignatura, eliminarAsignatura, eliminarPrograma } from "./program";

describe("desasociarAsignatura", () => {
  afterEach(() => vi.restoreAllMocks());

  it("envía la pareja correcta al endpoint DELETE", async () => {
    const borrar = vi.spyOn(apiClient, "delete").mockResolvedValue(undefined);

    await desasociarAsignatura("p1", "a1");

    expect(borrar).toHaveBeenCalledWith("/api/programas/p1/asignaturas/a1");
  });
});

it("elimina un programa por su identificador", async () => {
  const borrar = vi.spyOn(apiClient, "delete").mockResolvedValue(undefined);
  try {
    await eliminarPrograma("p1");
    expect(borrar).toHaveBeenCalledWith("/api/programas/p1");
  } finally {
    vi.restoreAllMocks();
  }
});

it("elimina una asignatura por su identificador", async () => {
  const borrar = vi.spyOn(apiClient, "delete").mockResolvedValue(undefined);
  try {
    await eliminarAsignatura("a1");
    expect(borrar).toHaveBeenCalledWith("/api/asignaturas/a1");
  } finally {
    vi.restoreAllMocks();
  }
});
