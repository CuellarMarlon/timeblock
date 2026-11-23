import { NavLink, Outlet } from "react-router-dom";
import "./AppLayout.css";

/**
 * Estructura visual común de Timeblock: barra lateral con la marca y la
 * navegación, y un área de contenido donde se muestra la página activa.
 * No contiene lógica de negocio ni llamadas a la API.
 */
export function AppLayout() {
  return (
    <div className="app-layout">
      <aside className="app-sidebar">
        <div className="app-brand">
          <span className="app-logo" aria-hidden="true">
            T
          </span>
          <p className="app-name">Timeblock</p>
        </div>

        <nav aria-label="Navegación principal">
          <ul className="app-nav">
            <li>
              <NavLink to="/" end>
                Inicio
              </NavLink>
            </li>
            <li>
              <NavLink to="/personas">Personas y perfiles</NavLink>
            </li>
            <li>
              <NavLink to="/programas">Programas y asignaturas</NavLink>
            </li>
            <li>
              <NavLink to="/matricula">Matrícula</NavLink>
            </li>
            <li>
              <NavLink to="/bloques">Bloques de contenido</NavLink>
            </li>
            <li>
              <NavLink to="/horarios">Horarios</NavLink>
            </li>
          </ul>
        </nav>

        <p className="app-description">Sistema para la gestión de horarios docentes.</p>
      </aside>

      <main className="app-content">
        <Outlet />
      </main>
    </div>
  );
}
