import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { NotificacionService } from '../services/notificacion.service';

const PUBLIC_AUTH_ENDPOINTS = [
  '/api/auth/login',
  '/api/auth/register',
  '/api/auth/refresh',
  '/api/auth/solicitar-reset',
  '/api/auth/reset-password'
];

let forcedLogoutInProgress = false;

function isPublicAuthEndpoint(url: string): boolean {
  return PUBLIC_AUTH_ENDPOINTS.some((endpoint) => url.includes(endpoint));
}

function isBlockedResponse(error: HttpErrorResponse): boolean {
  const code = String(error.error?.code ?? '').toUpperCase();
  const message = `${error.error?.message ?? ''} ${error.error?.error ?? ''}`.toLowerCase();

  return code === 'USER_BLOCKED' || message.includes('bloquead');
}

export const sessionInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const notificacion = inject(NotificacionService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      const esApi = req.url.includes('/api/');
      const esEndpointPublicoAuth = isPublicAuthEndpoint(req.url);
      const noAutorizado = error.status === 401 || error.status === 403;
      const habiaSesion = !!authService.getToken();

      if (esApi && !esEndpointPublicoAuth && noAutorizado && habiaSesion && !forcedLogoutInProgress) {
        forcedLogoutInProgress = true;

        const cuentaBloqueada = isBlockedResponse(error);
        authService.logout();

        if (cuentaBloqueada) {
          notificacion.error(
            'Cuenta bloqueada',
            'Tu cuenta ha sido bloqueada por un administrador. Contacta con soporte para mas informacion.'
          );
        } else {
          notificacion.aviso(
            'Sesion finalizada',
            'Tu sesion ha expirado o ya no es valida. Inicia sesion de nuevo para continuar.'
          );
        }

        router.navigate(['/']).finally(() => {
          forcedLogoutInProgress = false;
        });
      }

      return throwError(() => error);
    })
  );
};