import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "./client";
import { desasociarAsignatura } from "./program";

describe("desasociarAsignatura", () => {
  afterEach(() => vi.restoreAllMocks());

  it("envía la pareja correcta al endpoint DELETE", async () => {
    const borrar = vi.spyOn(apiClient, "delete").mockResolvedValue(undefined);

    await desasociarAsignatura("p1", "a1");

    expect(borrar).toHaveBeenCalledWith("/api/programas/p1/asignaturas/a1");
  });
});
