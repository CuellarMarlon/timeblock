import { useState } from "react";
import { Tabs } from "../components/Tabs";
import "./ProgramasPage.css";
import { ProgramasPanel } from "./ProgramasPanel";

const TABS = [
  { id: "programas", label: "Programas académicos" },
  { id: "asignaturas", label: "Asignaturas" },
];

/** Módulo de programas académicos y asignaturas, organizado en pestañas. */
export function ProgramasPage() {
  const [tab, setTab] = useState("programas");

  return (
    <section className="module" aria-labelledby="programas-titulo">
      <header>
        <h1 id="programas-titulo">Programas y asignaturas</h1>
        <p>Define los programas académicos y las asignaturas que los componen.</p>
      </header>

      <Tabs label="Secciones" tabs={TABS} active={tab} onChange={setTab} />

      <div role="tabpanel" id={`panel-${tab}`} aria-labelledby={`tab-${tab}`}>
        {tab === "programas" ? (
          <ProgramasPanel />
        ) : (
          <p className="notice">La gestión de asignaturas se añade en el siguiente paso.</p>
        )}
      </div>
    </section>
  );
}
