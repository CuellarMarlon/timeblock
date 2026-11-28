import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  actualizarAula,
  CAPACIDAD_MAX,
  crearAula,
  EDIFICIO_MAX_LENGTH,
  type Aula,
  type DatosAula,
} from "../../api/aulas";
import { useApiMutation } from "../../api/useApiMutation";
import { ErrorMessage } from "../components/ErrorMessage";
import "./BloquesHorarios.css";
import "./ProgramasPage.css";

interface AulaFormProps {
  aula?: Aula;
}

interface Errores {
  edificio?: string;
  capacidad?: string;
}

function validar(edificio: string, capacidad: string): Errores {
  const errores: Errores = {};
  const edificioLimpio = edificio.trim();
  const capacidadLimpia = capacidad.trim();

  if (!edificioLimpio) {
    errores.edificio = "El edificio es obligatorio.";
  } else if (edificioLimpio.length > EDIFICIO_MAX_LENGTH) {
    errores.edificio = `El edificio no puede superar los ${EDIFICIO_MAX_LENGTH} caracteres.`;
  }

  if (!capacidadLimpia) {
    errores.capacidad = "La capacidad es obligatoria.";
  } else if (!/^-?\d+$/.test(capacidadLimpia)) {
    errores.capacidad = "La capacidad debe ser un número entero.";
  } else if (Number(capacidadLimpia) < 1) {
    errores.capacidad = "La capacidad debe ser mayor que cero.";
  } else if (Number(capacidadLimpia) > CAPACIDAD_MAX) {
    errores.capacidad = `La capacidad no puede superar ${CAPACIDAD_MAX}.`;
  }

  return errores;
}

export function AulaForm({ aula }: AulaFormProps) {
  const navigate = useNavigate();
  const editando = aula !== undefined;

  const [edificio, setEdificio] = useState(aula?.edificio ?? "");
  const [capacidad, setCapacidad] = useState(aula ? String(aula.capacidad) : "");
  const [errores, setErrores] = useState<Errores>({});
  const [sinCambios, setSinCambios] = useState(false);

  const crear = useApiMutation(crearAula);
  const actualizar = useApiMutation(actualizarAula);
  const envio = editando ? actualizar.state : crear.state;
  const enviando = envio.status === "loading";

  async function enviar(event: FormEvent) {
    event.preventDefault();
    setSinCambios(false);

    const nuevos = validar(edificio, capacidad);
    setErrores(nuevos);
    if (Object.keys(nuevos).length > 0) return;

    const datos: DatosAula = { edificio: edificio.trim(), capacidad: Number(capacidad.trim()) };

    if (aula) {
      if (datos.edificio === aula.edificio && datos.capacidad === aula.capacidad) {
        setSinCambios(true);
        return;
      }
      const resultado = await actualizar.run(aula.id, datos);
      if (resultado.ok) {
        navigate("/aulas", { state: { aviso: "Aula actualizada correctamente." } });
      }
      return;
    }

    const resultado = await crear.run(datos);
    if (resultado.ok) {
      navigate("/aulas", { state: { aviso: "Aula creada correctamente." } });
    }
  }

  return (
    <form
      className="module-form card form-card form-card--estrecho"
      onSubmit={enviar}
      noValidate
      aria-label={editando ? "Editar aula" : "Nueva aula"}
    >
      <div className="field">
        <label>
          Edificio
          <input
            type="text"
            value={edificio}
            aria-invalid={errores.edificio ? true : undefined}
            aria-describedby={errores.edificio ? "aula-edificio-error" : undefined}
            onChange={(event) => setEdificio(event.target.value)}
          />
        </label>
        {errores.edificio && (
          <p className="field-error" id="aula-edificio-error">
            {errores.edificio}
          </p>
        )}
      </div>

      <div className="field">
        <label>
          Capacidad
          <input
            type="number"
            min={1}
            max={CAPACIDAD_MAX}
            step={1}
            value={capacidad}
            aria-invalid={errores.capacidad ? true : undefined}
            aria-describedby={errores.capacidad ? "aula-capacidad-error" : undefined}
            onChange={(event) => setCapacidad(event.target.value)}
          />
        </label>
        {errores.capacidad && (
          <p className="field-error" id="aula-capacidad-error">
            {errores.capacidad}
          </p>
        )}
      </div>

      {sinCambios && (
        <p className="notice" role="status">
          No hay cambios que guardar.
        </p>
      )}
      {envio.status === "error" && <ErrorMessage error={envio.error} />}

      <div className="form-actions">
        <Link className="btn" to="/aulas">
          Cancelar
        </Link>
        <button type="submit" className="btn btn-primary" disabled={enviando}>
          {enviando ? "Guardando…" : editando ? "Guardar cambios" : "Crear aula"}
        </button>
      </div>
    </form>
  );
}
