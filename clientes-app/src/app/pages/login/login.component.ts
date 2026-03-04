import { Component, EventEmitter, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService, LoginRequest } from '../../services/auth.service';
import { Router } from '@angular/router';

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

  email = signal('');
  password = signal('');
  isLoading = signal(false);
  errorMessage = signal('');

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
        // Redirigir o actualizar la UI según sea necesario
        window.location.reload(); // Recargar para actualizar el estado
      },
      error: (error) => {
        console.error('Error en login:', error);
        this.isLoading.set(false);
        this.errorMessage.set(error.error?.error || 'Error al iniciar sesión. Verifica tus credenciales.');
      }
    });
  }

  onClose(): void {
    this.close.emit();
  }

  onSwitchToRegister(): void {
    this.switchToRegister.emit();
  }
}
