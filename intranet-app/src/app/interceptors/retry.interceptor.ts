import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { retry, timer, catchError, throwError } from 'rxjs';

/**
 * Interceptor de reintento automático para peticiones HTTP.
 * Reintenta hasta 3 veces con backoff exponencial en errores de red (status 0, 502, 503, 504).
 * No reintenta errores de autenticación (401, 403) ni de validación (4xx).
 */
export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  // No reintentar para mutaciones (POST/PUT/DELETE) excepto status 0 (error de red)
  const esMutacion = ['POST', 'PUT', 'DELETE', 'PATCH'].includes(req.method);

  return next(req).pipe(
    retry({
      count: esMutacion ? 1 : 3,
      delay: (error: HttpErrorResponse, retryCount: number) => {
        // Solo reintentar en errores de red o servidor
        const reintentable = error.status === 0 || error.status === 502 ||
                             error.status === 503 || error.status === 504;

        if (!reintentable) {
          throw error;
        }

        // Backoff exponencial: 1s, 2s, 4s
        const delayMs = Math.pow(2, retryCount - 1) * 1000;
        console.warn(
          `[RetryInterceptor] Reintentando ${req.method} ${req.url} (intento ${retryCount}, espera ${delayMs}ms)`
        );

        return timer(delayMs);
      }
    }),
    catchError((error: HttpErrorResponse) => {
      return throwError(() => error);
    })
  );
};
