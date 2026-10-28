import { createBrowserRouter, type RouteObject } from "react-router-dom";
import { AppLayout } from "./layouts/AppLayout";
import { BloquesPage } from "./pages/BloquesPage";
import { HomePage } from "./pages/HomePage";
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
      { path: "horarios", element: <HorariosPage /> },
      { path: "*", element: <NotFoundPage /> },
    ],
  },
];

export const router = createBrowserRouter(routes);
