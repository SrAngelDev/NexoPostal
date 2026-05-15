import { Component, EventEmitter, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService, LoginRequest } from '../../services/auth.service';
import { Router } from '@angular/router';

type Vista = 'login' | 'forgot' | 'forgot-sent';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  @Output() close = new EventEmitter<void>();
  @Output() switchToRegister = new EventEmitter<void>();

  // Vista actual del modal
  vista = signal<Vista>('login');

  // Login
  email = signal('');
  password = signal('');
  isLoading = signal(false);
  errorMessage = signal('');

  // Recuperar contraseña
  forgotEmail = signal('');
  forgotLoading = signal(false);
  forgotError = signal('');

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  onSubmit(): void {
    if (!this.email() || !this.password()) {
      this.errorMessage.set('Por favor, completa todos los campos');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const credentials: LoginRequest = {
      email: this.email(),
      password: this.password()
    };

    this.authService.login(credentials).subscribe({
      next: (response) => {
        console.log('Login exitoso:', response);
        this.isLoading.set(false);
        this.close.emit();
        window.location.reload();
      },
      error: (error) => {
        console.error('Error en login:', error);
        this.isLoading.set(false);
        this.errorMessage.set(error.error?.error || 'Error al iniciar sesión. Verifica tus credenciales.');
      }
    });
  }

  onSolicitarReset(): void {
    const email = this.forgotEmail().trim();
    if (!email) {
      this.forgotError.set('Introduce tu dirección de email');
      return;
    }

    this.forgotLoading.set(true);
    this.forgotError.set('');

    this.authService.solicitarResetPassword(email).subscribe({
      next: () => {
        this.forgotLoading.set(false);
        this.vista.set('forgot-sent');
      },
      error: () => {
        this.forgotLoading.set(false);
        // Siempre mostramos éxito para no revelar si el email existe
        this.vista.set('forgot-sent');
      }
    });
  }

  onClose(): void {
    this.close.emit();
  }

  onSwitchToRegister(): void {
    this.switchToRegister.emit();
  }

  showForgot(): void {
    this.forgotEmail.set('');
    this.forgotError.set('');
    this.vista.set('forgot');
  }

  backToLogin(): void {
    this.vista.set('login');
    this.errorMessage.set('');
  }
}
