import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

type Vista = 'form' | 'success' | 'error';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.css'
})
export class ResetPasswordComponent implements OnInit {
  vista = signal<Vista>('form');

  email = signal('');
  token = signal('');

  nuevaPassword = signal('');
  confirmarPassword = signal('');
  isLoading = signal(false);
  errorMessage = signal('');

  showPassword = signal(false);
  showConfirmPassword = signal(false);

  passwordReqs = computed(() => {
    const p = this.nuevaPassword();
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

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const email = params.get('email');
    const token = params.get('token');

    if (!email || !token) {
      this.vista.set('error');
      return;
    }

    this.email.set(email);
    this.token.set(token);
  }

  onSubmit(): void {
    const nueva = this.nuevaPassword().trim();
    const confirmar = this.confirmarPassword().trim();

    if (!nueva || !confirmar) {
      this.errorMessage.set('Completa todos los campos');
      return;
    }

    const reqs = this.passwordReqs();
    if (!reqs.length || !reqs.uppercase || !reqs.lowercase || !reqs.number || !reqs.special) {
      this.errorMessage.set('La contraseña no cumple los requisitos de seguridad');
      return;
    }

    if (nueva !== confirmar) {
      this.errorMessage.set('Las contraseñas no coinciden');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.authService.resetPassword(this.email(), this.token(), nueva).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.vista.set('success');
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(
          err.error?.error || 'El enlace de recuperación no es válido o ha expirado.'
        );
      }
    });
  }
}
