import { useId } from "react";
import "./ui.css";

export interface SelectOption {
  value: string;
  label: string;
}

interface SelectFieldProps {
  label: string;
  value: string;
  options: SelectOption[];
  onChange: (value: string) => void;
  /** Texto de la opción vacía ("Todos", "Selecciona un programa"…). */
  placeholder: string;
  /** Mensaje de validación que se muestra bajo el campo. */
  error?: string;
  disabled?: boolean;
}

/** Selector con etiqueta accesible, opción vacía y mensaje de error opcional. */
export function SelectField({
  label,
  value,
  options,
  onChange,
  placeholder,
  error,
  disabled,
}: SelectFieldProps) {
  const id = useId();
  const errorId = `${id}-error`;

  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <select
        id={id}
        value={value}
        disabled={disabled}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        onChange={(event) => onChange(event.target.value)}
      >
        <option value="">{placeholder}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {error && (
        <p className="field-error" id={errorId}>
          {error}
        </p>
      )}
    </div>
  );
}
