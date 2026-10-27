import { NavLink, Outlet } from "react-router-dom";
import "./AppLayout.css";
/**
 * Estructura visual común de Timeblock.
 * Muestra el encabezado, la navegación y el área donde se renderiza
 * la página activa.
 */
export function AppLayout() {
  return (
    <div className="app-layout">
      <header className="app-header">
        <div className="app-brand">
          <p className="app-name">Timeblock</p>
          <p className="app-description">Sistema para la gestión de horarios docentes.</p>
        </div>
        <nav aria-label="Navegación principal">
          <ul className="app-nav">
            <li>
              <NavLink to="/" end>
                Inicio
              </NavLink>
            </li>
          </ul>
        </nav>
      </header>

      <main className="app-content">
        <Outlet />
      </main>
    </div>
  );
}
