import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * Interceptor HTTP que añade automáticamente el token JWT a las peticiones
 * Solo se aplica a peticiones hacia el backend de NexoPostal
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  
  // Solo añadimos el token a peticiones hacia nuestro backend
  if (req.url.includes('/api/')) {
    const token = authService.getToken();
    
    if (token) {
      // Clonamos la petición y añadimos el header Authorization
      const clonedRequest = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      
      return next(clonedRequest);
    }
  }
  
  // Si no hay token o no es una petición a nuestro backend, seguimos sin modificar
  return next(req);
};
