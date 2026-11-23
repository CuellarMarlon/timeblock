import { useState } from "react";
import { Tabs } from "../components/Tabs";
import "./ProgramasPage.css";
import { ProgramasPanel } from "./ProgramasPanel";
import { AsignaturasPanel } from "./AsignaturasPanel";
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
        {tab === "programas" ? <ProgramasPanel /> : <AsignaturasPanel />}
      </div>
    </section>
  );
}
