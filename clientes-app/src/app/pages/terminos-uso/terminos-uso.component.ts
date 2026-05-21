import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarPublicoComponent } from '../../components/navbar-publico/navbar-publico.component';
import { FooterPublicoComponent } from '../../components/footer-publico/footer-publico.component';

@Component({
  selector: 'app-terminos-uso',
  standalone: true,
  imports: [CommonModule, NavbarPublicoComponent, FooterPublicoComponent],
  templateUrl: './terminos-uso.component.html'
})
export class TerminosUsoComponent {
  constructor(private router: Router) {}

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
