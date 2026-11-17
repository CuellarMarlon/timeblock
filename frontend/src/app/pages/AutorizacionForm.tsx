import { useState, type FormEvent } from "react";
import { actualizarAutorizacion, crearAutorizacion, type Autorizacion } from "../../api/persons";
import type { Programa } from "../../api/program";
import { useApiMutation } from "../../api/useApiMutation";
import { ErrorMessage } from "../components/ErrorMessage";

interface AutorizacionFormProps {
  coordinadorId: string;
  programas: Programa[];
  /** Autorización a actualizar. Si no se indica, se crea una nueva. */
  autorizacion?: Autorizacion;
  onSaved: () => void;
  onCancel: () => void;
}

type FieldErrors = Partial<Record<"programaId" | "fechaInicio" | "fechaFin", string>>;

interface FormValues {
  programaId: string;
  fechaInicio: string;
  fechaFin: string | null;
  puedeGenerar: boolean;
  puedeValidar: boolean;
}

/** Formulario de creación o actualización de una autorización de coordinador. */
export function AutorizacionForm({
  coordinadorId,
  programas,
  autorizacion,
  onSaved,
  onCancel,
}: AutorizacionFormProps) {
  const [programaId, setProgramaId] = useState(autorizacion?.programaId ?? "");
  const [fechaInicio, setFechaInicio] = useState(autorizacion?.fechaInicio ?? "");
  const [fechaFin, setFechaFin] = useState(autorizacion?.fechaFin ?? "");
  const [puedeGenerar, setPuedeGenerar] = useState(autorizacion?.puedeGenerar ?? false);
  const [puedeValidar, setPuedeValidar] = useState(autorizacion?.puedeValidar ?? false);
  const [errors, setErrors] = useState<FieldErrors>({});

  const guardar = useApiMutation((v: FormValues) =>
    autorizacion
      ? actualizarAutorizacion(coordinadorId, autorizacion.programaId, autorizacion.fechaInicio, {
          fechaFin: v.fechaFin,
          puedeGenerar: v.puedeGenerar,
          puedeValidar: v.puedeValidar,
        })
      : crearAutorizacion(coordinadorId, v),
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const nuevos: FieldErrors = {};
    if (!programaId) nuevos.programaId = "Selecciona un programa.";
    if (!fechaInicio) nuevos.fechaInicio = "La fecha de inicio es obligatoria.";
    if (fechaFin && fechaInicio && fechaFin < fechaInicio) {
      nuevos.fechaFin = "La fecha de fin no puede ser anterior a la de inicio.";
    }
    setErrors(nuevos);
    if (Object.keys(nuevos).length > 0) return;

    const ok = await guardar.submit({
      programaId,
      fechaInicio,
      fechaFin: fechaFin || null,
      puedeGenerar,
      puedeValidar,
    });
    if (ok) onSaved();
  }

  const editando = autorizacion !== undefined;

  return (
    <form className="module-form" onSubmit={handleSubmit} noValidate>
      <label>
        Programa académico
        <select
          value={programaId}
          disabled={editando}
          onChange={(e) => setProgramaId(e.target.value)}
          aria-invalid={errors.programaId ? true : undefined}
        >
          <option value="">Selecciona un programa</option>
          {programas.map((p) => (
            <option key={p.id} value={p.id}>
              {p.nombre}
            </option>
          ))}
        </select>
      </label>
      {errors.programaId && <p className="field-error">{errors.programaId}</p>}

      <label>
        Fecha de inicio
        <input
          type="date"
          value={fechaInicio}
          disabled={editando}
          onChange={(e) => setFechaInicio(e.target.value)}
          aria-invalid={errors.fechaInicio ? true : undefined}
        />
      </label>
      {errors.fechaInicio && <p className="field-error">{errors.fechaInicio}</p>}

      <label>
        Fecha de fin (opcional)
        <input
          type="date"
          value={fechaFin}
          onChange={(e) => setFechaFin(e.target.value)}
          aria-invalid={errors.fechaFin ? true : undefined}
        />
      </label>
      {errors.fechaFin && <p className="field-error">{errors.fechaFin}</p>}

      <label className="check">
        <input
          type="checkbox"
          checked={puedeGenerar}
          onChange={(e) => setPuedeGenerar(e.target.checked)}
        />
        Puede generar horarios
      </label>
      <label className="check">
        <input
          type="checkbox"
          checked={puedeValidar}
          onChange={(e) => setPuedeValidar(e.target.checked)}
        />
        Puede validar horarios
      </label>

      {guardar.status === "error" && <ErrorMessage error={guardar.error} />}

      <div className="form-actions">
        <button type="button" className="btn" onClick={onCancel}>
          Cancelar
        </button>
        <button type="submit" className="btn btn-primary" disabled={guardar.status === "pending"}>
          {guardar.status === "pending" ? "Guardando…" : "Guardar autorización"}
        </button>
      </div>
    </form>
  );
}
