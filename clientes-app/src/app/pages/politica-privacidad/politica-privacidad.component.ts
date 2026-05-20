import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { NavbarPublicoComponent } from '../../components/navbar-publico/navbar-publico.component';

@Component({
  selector: 'app-politica-privacidad',
  standalone: true,
  imports: [CommonModule, NavbarPublicoComponent],
  templateUrl: './politica-privacidad.component.html'
})
export class PoliticaPrivacidadComponent {
  constructor(private router: Router) {}

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
