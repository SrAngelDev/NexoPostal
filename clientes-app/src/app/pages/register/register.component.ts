import { Component, EventEmitter, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService, RegisterRequest } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  @Output() close = new EventEmitter<void>();
  @Output() switchToLogin = new EventEmitter<void>();

  email = signal('');
  password = signal('');
  confirmPassword = signal('');
  nombreCompleto = signal('');
  isLoading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  onSubmit(): void {
    // Validaciones
    if (!this.email() || !this.password() || !this.confirmPassword() || !this.nombreCompleto()) {
      this.errorMessage.set('Por favor, completa todos los campos');
      return;
    }

    if (this.password() !== this.confirmPassword()) {
      this.errorMessage.set('Las contraseñas no coinciden');
      return;
    }

    if (this.password().length < 6) {
      this.errorMessage.set('La contraseña debe tener al menos 6 caracteres');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    const registerData: RegisterRequest = {
      email: this.email(),
      password: this.password(),
      nombreCompleto: this.nombreCompleto()
    };

    this.authService.register(registerData).subscribe({
      next: (response) => {
        console.log('Registro exitoso:', response);
        this.isLoading.set(false);
        this.successMessage.set('¡Cuenta creada exitosamente! Redirigiendo...');
        
        // Esperar 2 segundos y cerrar el modal
        setTimeout(() => {
          this.close.emit();
          window.location.reload(); // Recargar para actualizar el estado
        }, 2000);
      },
      error: (error) => {
        console.error('Error en registro:', error);
        this.isLoading.set(false);
        
        if (error.error?.errors) {
          this.errorMessage.set(error.error.errors.join(', '));
        } else {
          this.errorMessage.set(error.error?.message || 'Error al registrar. Intenta de nuevo.');
        }
      }
    });
  }

  onClose(): void {
    this.close.emit();
  }

  onSwitchToLogin(): void {
    this.switchToLogin.emit();
  }
}
