import { createBrowserRouter, type RouteObject } from "react-router-dom";
import { AppLayout } from "./layouts/AppLayout";
import { AulaEditarPage } from "./pages/AulaEditarPage";
import { AulaNuevaPage } from "./pages/AulaNuevaPage";
import { AulasPage } from "./pages/AulasPage";
import { BloqueDetallePage } from "./pages/BloqueDetallePage";
import { BloqueEditarPage } from "./pages/BloqueEditarPage";
import { BloqueNuevoPage } from "./pages/BloqueNuevoPage";
import { BloquesPage } from "./pages/BloquesPage";
import { HomePage } from "./pages/HomePage";
import { HorarioDetallePage } from "./pages/HorarioDetallePage";
import { HorarioNuevoPage } from "./pages/HorarioNuevoPage";
import { HorariosPage } from "./pages/HorariosPage";
import { MatriculaPage } from "./pages/MatriculaPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { PersonasPage } from "./pages/PersonasPage";
import { ProgramasPage } from "./pages/ProgramasPage";

/** Definición de rutas de la aplicación. Se exporta para poder probarla. */
export const routes: RouteObject[] = [
  {
    path: "/",
    element: <AppLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: "personas", element: <PersonasPage /> },
      { path: "programas", element: <ProgramasPage /> },
      { path: "matricula", element: <MatriculaPage /> },
      { path: "bloques", element: <BloquesPage /> },
      { path: "bloques/nuevo", element: <BloqueNuevoPage /> },
      { path: "bloques/:bloqueId", element: <BloqueDetallePage /> },
      { path: "bloques/:bloqueId/editar", element: <BloqueEditarPage /> },
      { path: "horarios", element: <HorariosPage /> },
      { path: "horarios/nuevo", element: <HorarioNuevoPage /> },
      { path: "horarios/:horarioId", element: <HorarioDetallePage /> },
      { path: "aulas", element: <AulasPage /> },
      { path: "aulas/nueva", element: <AulaNuevaPage /> },
      { path: "aulas/:aulaId/editar", element: <AulaEditarPage /> },
      { path: "*", element: <NotFoundPage /> },
    ],
  },
];

export const router = createBrowserRouter(routes);
