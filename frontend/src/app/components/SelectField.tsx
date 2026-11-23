import { useId } from "react";

export interface SelectOption {
  value: string;
  label: string;
}

interface SelectFieldProps {
  label: string;
  value: string;
  options: SelectOption[];
  onChange: (value: string) => void;
  // Texto de la opción vacía ("Selecciona un programa", por ejemplo)
  placeholder: string;
  // Mensaje de validación que se muestra debajo del campo
  error?: string;
  disabled?: boolean;
}

// Selector para formularios: la etiqueta envuelve al select (igual que en el resto
// de formularios del proyecto) y el error sale debajo, fuera de la etiqueta
export function SelectField({
  label,
  value,
  options,
  onChange,
  placeholder,
  error,
  disabled,
}: SelectFieldProps) {
  const errorId = `${useId()}-error`;

  return (
    <div className="field">
      <label>
        {label}
        <select
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
      </label>
      {error && (
        <p className="field-error" id={errorId}>
          {error}
        </p>
      )}
    </div>
  );
}
