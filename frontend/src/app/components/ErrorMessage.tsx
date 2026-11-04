import { describeError } from "../../api/errors";
import "./feedback.css";

interface ErrorMessageProps {
  error: unknown;
  /** Si se indica, muestra un botón para reintentar. */
  onRetry?: () => void;
}

/**
 * Muestra un error de la API de forma comprensible: título, detalle y,
 * si los hay, los errores de validación por campo.
 */
export function ErrorMessage({ error, onRetry }: ErrorMessageProps) {
  const { title, detail, fieldErrors } = describeError(error);
  const campos = Object.entries(fieldErrors);

  return (
    <div className="feedback feedback--error" role="alert">
      <p className="feedback__title">{title}</p>
      {detail && <p>{detail}</p>}
      {campos.length > 0 && (
        <ul>
          {campos.flatMap(([campo, mensajes]) =>
            mensajes.map((mensaje) => <li key={`${campo}-${mensaje}`}>{mensaje}</li>),
          )}
        </ul>
      )}
      {onRetry && (
        <button type="button" onClick={onRetry}>
          Reintentar
        </button>
      )}
    </div>
  );
}
