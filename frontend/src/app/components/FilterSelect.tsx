import type { SelectOption } from "./SelectField";

interface FilterSelectProps {
  // Texto para lectores de pantalla, por ejemplo "Filtrar por programa"
  label: string;
  value: string;
  options: SelectOption[];
  onChange: (value: string) => void;
  // Texto de la opción vacía, la que quita el filtro ("Todos los programas")
  placeholder: string;
}

// Selector compacto para las barras de filtros. No muestra etiqueta visible:
// la primera opción ya dice qué se está filtrando
export function FilterSelect({ label, value, options, onChange, placeholder }: FilterSelectProps) {
  return (
    <select aria-label={label} value={value} onChange={(event) => onChange(event.target.value)}>
      <option value="">{placeholder}</option>
      {options.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </select>
  );
}
