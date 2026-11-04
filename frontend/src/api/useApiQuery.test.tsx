import { describe, expect, it, vi } from "vitest";
import { act, renderHook, waitFor } from "@testing-library/react";
import { ApiError } from "./errors";
import { useApiQuery } from "./useApiQuery";

describe("useApiQuery", () => {
  it("pasa de carga a éxito con los datos", async () => {
    const load = vi.fn(() => Promise.resolve(["a"]));

    const { result } = renderHook(() => useApiQuery(load));

    expect(result.current.status).toBe("loading");
    await waitFor(() => expect(result.current).toMatchObject({ status: "success", data: ["a"] }));
  });

  it("expone el error y permite reintentar", async () => {
    const load = vi
      .fn<(signal: AbortSignal) => Promise<string[]>>()
      .mockRejectedValueOnce(new ApiError(500))
      .mockResolvedValueOnce(["b"]);

    const { result } = renderHook(() => useApiQuery(load));

    await waitFor(() => expect(result.current.status).toBe("error"));
    act(() => result.current.reload());
    expect(result.current.status).toBe("loading");
    await waitFor(() => expect(result.current).toMatchObject({ status: "success", data: ["b"] }));
    expect(load).toHaveBeenCalledTimes(2);
  });

  it("cancela la consulta al desmontar", () => {
    let signal: AbortSignal | undefined;
    const load = (s: AbortSignal) => {
      signal = s;
      return new Promise<never>(() => {});
    };

    const { unmount } = renderHook(() => useApiQuery(load));
    unmount();

    expect(signal?.aborted).toBe(true);
  });
});
