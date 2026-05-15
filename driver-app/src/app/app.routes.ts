import { Routes } from '@angular/router';
import { authGuard, loginGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent),
    canActivate: [loginGuard]
  },
  {
    path: 'ruta',
    loadComponent: () => import('./pages/ruta/ruta.component').then(m => m.RutaComponent),
    canActivate: [authGuard]
  },
  {
    path: 'escaneo',
    loadComponent: () => import('./pages/escaneo/escaneo.component').then(m => m.EscaneoComponent),
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
