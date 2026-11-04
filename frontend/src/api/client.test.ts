import { describe, expect, it, vi } from "vitest";
import { createApiClient } from "./client";
import { ApiError } from "./errors";
import type { PagedResult } from "./types";

function jsonResponse(body: unknown, status = 200, contentType = "application/json") {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": contentType } });
}

function clientWith(response: Response | Error | DOMException) {
  const fetchMock = vi.fn<typeof fetch>(() =>
    response instanceof Response ? Promise.resolve(response) : Promise.reject(response),
  );
  const client = createApiClient({ baseUrl: "http://api.test/", fetch: fetchMock });
  return { client, fetchMock };
}

describe("createApiClient", () => {
  it("une la URL base con la ruta y omite los parámetros vacíos", async () => {
    const { client, fetchMock } = clientWith(jsonResponse([]));

    await client.get("/api/personas", { query: { page: 2, pageSize: 20, tipo: undefined } });

    expect(fetchMock).toHaveBeenCalledWith(
      "http://api.test/api/personas?page=2&pageSize=20",
      expect.objectContaining({ method: "GET", body: undefined }),
    );
  });

  it("devuelve el cuerpo JSON de una respuesta paginada", async () => {
    const pagina: PagedResult<{ id: string }> = {
      items: [{ id: "1" }],
      page: 1,
      pageSize: 20,
      totalItems: 1,
    };
    const { client } = clientWith(jsonResponse(pagina));

    await expect(client.get("/api/personas")).resolves.toEqual(pagina);
  });

  it("envía el cuerpo como JSON", async () => {
    const { client, fetchMock } = clientWith(jsonResponse({ id: "1" }, 201));

    await client.post("/api/programas", { nombre: "Ciencia de la Computación" });

    const [, init] = fetchMock.mock.calls[0];
    expect(init?.method).toBe("POST");
    expect(init?.body).toBe(JSON.stringify({ nombre: "Ciencia de la Computación" }));
    expect(init?.headers).toMatchObject({ "Content-Type": "application/json" });
  });

  it("resuelve undefined en una respuesta 204", async () => {
    const { client } = clientWith(new Response(null, { status: 204 }));

    await expect(client.put("/api/programas/1/asignaturas/2")).resolves.toBeUndefined();
  });

  it("lanza ApiError con el Problem Details de la respuesta", async () => {
    const problema = {
      type: "about:blank",
      title: "La solicitud contiene datos inválidos",
      status: 400,
      detail: "Revisa los campos indicados.",
      errors: { edad: ["La edad debe ser mayor que cero."] },
    };
    const { client } = clientWith(jsonResponse(problema, 400, "application/problem+json"));

    const error = await client.post("/api/personas", {}).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({
      status: 400,
      message: "Revisa los campos indicados.",
      problem: problema,
      fieldErrors: problema.errors,
    });
  });

  it("lanza ApiError con mensaje por defecto si el error no trae cuerpo", async () => {
    const { client } = clientWith(new Response(null, { status: 404 }));

    const error = await client.get("/api/personas/x").catch((e: unknown) => e);

    expect(error).toMatchObject({ status: 404, problem: undefined });
    expect((error as ApiError).message).toBe("El recurso solicitado no existe.");
  });

  it("convierte un fallo de red en ApiError con estado 0", async () => {
    const { client } = clientWith(new TypeError("Failed to fetch"));

    const error = await client.get("/api/personas").catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).isNetworkError).toBe(true);
  });

  it("propaga la cancelación sin convertirla en ApiError", async () => {
    const { client } = clientWith(new DOMException("Aborted", "AbortError"));

    await expect(client.get("/api/personas")).rejects.toMatchObject({ name: "AbortError" });
  });
});
