import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  nombreCompleto: string;
}

export interface AuthResponse {
  token: string;
  expiration: Date;
  user: string;
  rol: string;
}

export interface Usuario {
  user: string;
  rol: string;
}

export interface SolicitarResetRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  nuevaPassword: string;
}

export interface SolicitarResetRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  nuevaPassword: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = '/api/auth';
  private readonly TOKEN_KEY = 'nexopostal_token';
  private readonly USER_KEY = 'nexopostal_user';
  
  private currentUserSubject = new BehaviorSubject<Usuario | null>(this.getUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {}

  /**
   * Inicia sesión con email y contraseña
   */
  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/login`, credentials).pipe(
      tap(response => {
        // Validar que el usuario tenga el rol Cliente
        if (response.rol !== 'Cliente') {
          throw new Error('Acceso denegado: Solo los clientes pueden acceder a esta aplicación');
        }
        this.storeAuthData(response);
      })
    );
  }

  /**
   * Registra un nuevo usuario
   */
  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/register`, data).pipe(
      tap(response => {
        this.storeAuthData(response);
      })
    );
  }

  /**
   * Cierra la sesión del usuario
   */
  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUserSubject.next(null);
  }

  /**
   * Obtiene el token JWT almacenado
   */
  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Verifica si el usuario está autenticado
   */
  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;
    
    // Verificar si el token no ha expirado
    return !this.isTokenExpired(token);
  }

  /**
   * Obtiene el usuario actual
   */
  getCurrentUser(): Usuario | null {
    return this.currentUserSubject.value;
  }

  /**
   * Verifica si el usuario tiene un rol específico
   */
  hasRole(role: string): boolean {
    const user = this.getCurrentUser();
    return user?.rol === role;
  }

  /**
   * Almacena los datos de autenticación
   */
  private storeAuthData(response: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    
    const user: Usuario = {
      user: response.user,
      rol: response.rol
    };
    
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  /**
   * Recupera el usuario desde localStorage
   */
  private getUserFromStorage(): Usuario | null {
    const userJson = localStorage.getItem(this.USER_KEY);
    if (!userJson) return null;
    
    try {
      return JSON.parse(userJson);
    } catch {
      return null;
    }
  }

  /**
   * Verifica si el token JWT ha expirado
   */
  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expiry = payload.exp;
      
      // exp está en segundos, Date.now() en milisegundos
      return (Math.floor((new Date()).getTime() / 1000)) >= expiry;
    } catch {
      return true;
    }
  }

  /**
   * Solicita el envío del email de recuperación de contraseña.
   * Envía window.location.origin para que el backend genere el enlace correcto
   * independientemente del entorno (local, staging, producción).
   */
  solicitarResetPassword(email: string): Observable<{ mensaje: string }> {
    return this.http.post<{ mensaje: string }>(`${this.API_URL}/solicitar-reset`, {
      email,
      frontendUrl: window.location.origin
    });
  }

  /**
   * Restablece la contraseña con el token recibido por email
   */
  resetPassword(email: string, token: string, nuevaPassword: string): Observable<{ mensaje: string }> {
    return this.http.post<{ mensaje: string }>(`${this.API_URL}/reset-password`, { email, token, nuevaPassword });
  }
}
