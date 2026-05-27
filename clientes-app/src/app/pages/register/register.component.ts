import { Component, EventEmitter, Output, signal, computed } from '@angular/core';
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

  showPassword = signal(false);
  showConfirmPassword = signal(false);

  // Requisitos de contraseña evaluados en tiempo real
  passwordReqs = computed(() => {
    const p = this.password();
    return {
      length:    p.length >= 8,
      uppercase: /[A-Z]/.test(p),
      lowercase: /[a-z]/.test(p),
      number:    /[0-9]/.test(p),
      special:   /[^A-Za-z0-9]/.test(p)
    };
  });

  passwordStrength = computed(() => {
    const met = Object.values(this.passwordReqs()).filter(Boolean).length;
    if (met <= 2) return 'weak';
    if (met <= 3) return 'fair';
    return 'strong';
  });

  private emailValido(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
  }

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

    if (this.nombreCompleto().trim().length < 2) {
      this.errorMessage.set('El nombre debe tener al menos 2 caracteres');
      return;
    }

    if (!this.emailValido(this.email())) {
      this.errorMessage.set('Introduce un correo electrónico válido');
      return;
    }

    const reqs = this.passwordReqs();
    if (!reqs.length) {
      this.errorMessage.set('La contraseña debe tener al menos 8 caracteres');
      return;
    }
    if (!reqs.uppercase) {
      this.errorMessage.set('La contraseña debe incluir al menos una letra mayúscula');
      return;
    }
    if (!reqs.lowercase) {
      this.errorMessage.set('La contraseña debe incluir al menos una letra minúscula');
      return;
    }
    if (!reqs.number) {
      this.errorMessage.set('La contraseña debe incluir al menos un número');
      return;
    }
    if (!reqs.special) {
      this.errorMessage.set('La contraseña debe incluir al menos un carácter especial (!@#$%...)');
      return;
    }

    if (this.password() !== this.confirmPassword()) {
      this.errorMessage.set('Las contraseñas no coinciden');
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
