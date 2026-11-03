import "./feedback.css";

interface EmptyStateProps {
  message?: string;
}

/** Mensaje para listados o consultas sin resultados. */
export function EmptyState({ message = "No hay resultados." }: EmptyStateProps) {
  return (
    <p className="feedback feedback--empty" role="status">
      {message}
    </p>
  );
}
