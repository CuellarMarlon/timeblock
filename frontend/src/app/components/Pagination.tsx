interface PaginationProps {
  page: number;
  pageSize: number;
  totalItems: number;
  onPageChange: (page: number) => void;
  // Nombre de la navegación para lectores de pantalla, p. ej. "Paginación de bloques"
  label?: string;
}

// Pie de tarjeta con los botones Anterior y Siguiente. Va dentro del `.card` de la tabla
export function Pagination({
  page,
  pageSize,
  totalItems,
  onPageChange,
  label = "Paginación",
}: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));

  return (
    <nav className="card-footer" aria-label={label}>
      <button
        type="button"
        className="btn"
        onClick={() => onPageChange(page - 1)}
        disabled={page <= 1}
      >
        Anterior
      </button>
      <span>
        Página {page} de {totalPages}
      </span>
      <button
        type="button"
        className="btn"
        onClick={() => onPageChange(page + 1)}
        disabled={page >= totalPages}
      >
        Siguiente
      </button>
    </nav>
  );
}
