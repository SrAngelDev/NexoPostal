import { Routes } from '@angular/router';
import { authGuard, loginGuard, jefeRepartoGuard, repartidorGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent),
    canActivate: [loginGuard]
  },
  {
    path: 'mis-rutas',
    loadComponent: () => import('./pages/mis-rutas/mis-rutas.component').then(m => m.MisRutasComponent),
    canActivate: [repartidorGuard]
  },
  {
    path: 'ruta',
    loadComponent: () => import('./pages/ruta/ruta.component').then(m => m.RutaComponent),
    canActivate: [repartidorGuard]
  },
  {
    path: 'ruta/:id',
    loadComponent: () => import('./pages/ruta/ruta.component').then(m => m.RutaComponent),
    canActivate: [repartidorGuard]
  },
  {
    path: 'escaneo',
    loadComponent: () => import('./pages/escaneo/escaneo.component').then(m => m.EscaneoComponent),
    canActivate: [repartidorGuard]
  },
  {
    path: 'gestion-rutas',
    loadComponent: () => import('./pages/gestion-rutas/gestion-rutas.component').then(m => m.GestionRutasComponent),
    canActivate: [jefeRepartoGuard]
  },
  {
    path: 'detalle-ruta/:id',
    loadComponent: () => import('./pages/detalle-ruta/detalle-ruta.component').then(m => m.DetalleRutaComponent),
    canActivate: [jefeRepartoGuard]
  },
  {
    path: 'dashboard-jefe',
    loadComponent: () => import('./pages/dashboard-jefe/dashboard-jefe.component').then(m => m.DashboardJefeComponent),
    canActivate: [jefeRepartoGuard]
  },
  {
    path: 'mapa-tiempo-real',
    loadComponent: () => import('./pages/mapa-tiempo-real/mapa-tiempo-real.component').then(m => m.MapaTiempoRealComponent),
    canActivate: [jefeRepartoGuard]
  },
  {
    path: 'asignar-paradas',
    loadComponent: () => import('./pages/asignar-paradas/asignar-paradas.component').then(m => m.AsignarParadasComponent),
    canActivate: [jefeRepartoGuard]
  },
  {
    path: 'bandeja-jefe',
    loadComponent: () => import('./pages/bandeja-jefe/bandeja-jefe.component').then(m => m.BandejaJefeComponent),
    canActivate: [jefeRepartoGuard]
  },
  {
    path: 'mis-repartidores',
    loadComponent: () => import('./pages/mis-repartidores/mis-repartidores.component').then(m => m.MisRepartidoresComponent),
    canActivate: [jefeRepartoGuard]
  },
  {
    path: '',
    loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
