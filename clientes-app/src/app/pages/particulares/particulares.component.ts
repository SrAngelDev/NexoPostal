import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { NavbarPublicoComponent } from '../../components/navbar-publico/navbar-publico.component';

@Component({
  selector: 'app-particulares',
  standalone: true,
  imports: [CommonModule, NavbarPublicoComponent],
  templateUrl: './particulares.component.html'
})
export class ParticularesComponent {
  constructor(private router: Router) {}

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
