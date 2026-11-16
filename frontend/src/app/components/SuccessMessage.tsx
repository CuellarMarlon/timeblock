import "./ui.css";

interface SuccessMessageProps {
  message: string;
}

/** Aviso de que una operación terminó correctamente. */
export function SuccessMessage({ message }: SuccessMessageProps) {
  return (
    <p className="notice notice--success" role="status">
      {message}
    </p>
  );
}
