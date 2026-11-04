import "./feedback.css";

interface LoadingIndicatorProps {
  label?: string;
}

/** Aviso accesible de que se están cargando datos. */
export function LoadingIndicator({ label = "Cargando…" }: LoadingIndicatorProps) {
  return (
    <p className="feedback feedback--loading" role="status" aria-live="polite">
      {label}
    </p>
  );
}
