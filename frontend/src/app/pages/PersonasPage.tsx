import { PersonasPanel } from "./PersonasPanel";
import "./ProgramasPage.css";

/** Módulo de personas y perfiles. */
export function PersonasPage() {
  return (
    <section className="module" aria-labelledby="personas-titulo">
      <header>
        <h1 id="personas-titulo">Personas y perfiles</h1>
        <p>Gestión de estudiantes, coordinadores académicos y jefes de departamento.</p>
      </header>

      <PersonasPanel />
    </section>
  );
}
