// Da formato a una fecha ISO con la zona horaria del navegador.
// Si la fecha no es válida, la deja como llegó
export function formatearFechaHora(iso: string): string {
  const fecha = new Date(iso);
  return Number.isNaN(fecha.getTime()) ? iso : fecha.toLocaleString("es");
}
