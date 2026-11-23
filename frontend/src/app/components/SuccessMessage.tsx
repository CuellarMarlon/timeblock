interface SuccessMessageProps {
  message: string;
}

// Aviso de que una operación terminó bien. Usa el mismo estilo `.notice` del resto de módulos
export function SuccessMessage({ message }: SuccessMessageProps) {
  return (
    <p className="notice" role="status">
      {message}
    </p>
  );
}
