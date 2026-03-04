import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';

export interface LoginRequest {
  email: string;
  password: string;
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

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = '/api/auth';
  private readonly TOKEN_KEY = 'nexopostal_intranet_token';
  private readonly USER_KEY = 'nexopostal_intranet_user';
  
  private currentUserSubject = new BehaviorSubject<Usuario | null>(this.getUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {}

  private readonly ROLES_PERMITIDOS = ['Admin', 'OperarioOficina', 'OperarioLogistico', 'OperarioJefe'];

  /**
   * Inicia sesión con email y contraseña
   * Solo permite acceso a usuarios con roles de intranet
   */
  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/login`, credentials).pipe(
      tap(response => {
        if (!this.ROLES_PERMITIDOS.includes(response.rol)) {
          throw new Error('Acceso denegado: No tienes permisos para acceder a la intranet');
        }
        this.storeAuthData(response);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;
    return !this.isTokenExpired(token);
  }

  getCurrentUser(): Usuario | null {
    return this.currentUserSubject.value;
  }

  getRol(): string | null {
    return this.getCurrentUser()?.rol ?? null;
  }

  isAdmin(): boolean {
    return this.getRol() === 'Admin';
  }

  isOperario(): boolean {
    const rol = this.getRol();
    return rol === 'OperarioOficina' || rol === 'OperarioLogistico' || rol === 'OperarioJefe';
  }

  isOperarioOficina(): boolean {
    return this.getRol() === 'OperarioOficina';
  }

  isOperarioLogistico(): boolean {
    return this.getRol() === 'OperarioLogistico';
  }

  isOperarioJefe(): boolean {
    return this.getRol() === 'OperarioJefe';
  }

  private storeAuthData(response: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    
    const user: Usuario = {
      user: response.user,
      rol: response.rol
    };
    
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  private getUserFromStorage(): Usuario | null {
    const userJson = localStorage.getItem(this.USER_KEY);
    if (!userJson) return null;
    
    try {
      return JSON.parse(userJson);
    } catch {
      return null;
    }
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expiry = payload.exp;
      return (Math.floor((new Date()).getTime() / 1000)) >= expiry;
    } catch {
      return true;
    }
  }
}
