/** Formatea una marca de tiempo ISO en la zona horaria del navegador. */
export function formatearFechaHora(iso: string): string {
  const fecha = new Date(iso);
  return Number.isNaN(fecha.getTime()) ? iso : fecha.toLocaleString("es");
}
