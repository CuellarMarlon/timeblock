import { useId } from "react";
import { RequierePermisoAulas } from "../components/RequierePermisoAulas";
import { AulaForm } from "./AulaForm";
import "./BloquesHorarios.css";
import "./ProgramasPage.css";

export function AulaNuevaPage() {
  const headingId = useId();

  return (
    <section className="module" aria-labelledby={headingId}>
      <header className="page-header">
        <div>
          <h1 id={headingId}>Nueva aula</h1>
          <p>Indica el edificio y la capacidad del aula.</p>
        </div>
      </header>

      <RequierePermisoAulas>
        <AulaForm />
      </RequierePermisoAulas>
    </section>
  );
}
