import { Routes } from '@angular/router';
import { authGuard, loginGuard, adminGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent),
    canActivate: [loginGuard]
  },
  {
    path: 'admin',
    loadComponent: () => import('./pages/admin-panel/admin-panel.component').then(m => m.AdminPanelComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'seguimiento-interno',
    loadComponent: () => import('./pages/seguimiento-interno/seguimiento-interno.component').then(m => m.SeguimientoInternoComponent),
    canActivate: [authGuard]
  },
  {
    path: 'gestion-cta',
    loadComponent: () => import('./pages/gestion-cta/gestion-cta.component').then(m => m.GestionCtaComponent),
    canActivate: [authGuard]
  },
  {
    path: 'asignaciones',
    loadComponent: () => import('./pages/asignaciones/asignaciones.component').then(m => m.AsignacionesComponent),
    canActivate: [authGuard]
  },
  {
    path: 'escaneo',
    loadComponent: () => import('./pages/escaneo/escaneo.component').then(m => m.EscaneoComponent),
    canActivate: [authGuard]
  },
  {
    path: 'alta-en-oficina',
    loadComponent: () => import('./pages/alta-en-oficina/alta-en-oficina.component').then(m => m.AltaEnOficinaComponent),
    canActivate: [authGuard]
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
