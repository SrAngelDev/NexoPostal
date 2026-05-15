import { Component, OnInit, signal } from '@angular/core';
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

    if (nueva.length < 6) {
      this.errorMessage.set('La contraseña debe tener al menos 6 caracteres');
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
