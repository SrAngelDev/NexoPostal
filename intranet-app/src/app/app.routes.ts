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
    // Ruta deprecada: el escaneo ahora vive integrado dentro de Asignaciones.
    path: 'escaneo',
    redirectTo: 'asignaciones',
    pathMatch: 'full'
  },
  {
    path: 'gestion-usuarios/:id',
    loadComponent: () => import('./pages/usuario-detalle/usuario-detalle.component').then(m => m.UsuarioDetalleComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'gestion-usuarios',
    loadComponent: () => import('./pages/gestion-usuarios/gestion-usuarios.component').then(m => m.GestionUsuariosComponent),
    canActivate: [adminGuard]
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
