import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  router.navigate(['/login']);
  return false;
};

export const loginGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  // Si ya está autenticado, redirigir al dashboard
  router.navigate(['/']);
  return false;
};

export const jefeRepartoGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/login']);
    return false;
  }

  if (authService.isJefeReparto()) {
    return true;
  }

  // No es JefeReparto, redirigir al dashboard
  router.navigate(['/']);
  return false;
};

export const repartidorGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/login']);
    return false;
  }

  if (authService.isRepartidor()) {
    return true;
  }

  // El JefeReparto no opera: lo enviamos a su panel
  if (authService.isJefeReparto()) {
    router.navigate(['/dashboard-jefe']);
    return false;
  }

  router.navigate(['/']);
  return false;
};
