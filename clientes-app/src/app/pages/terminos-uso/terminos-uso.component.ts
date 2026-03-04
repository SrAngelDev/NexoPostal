import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-terminos-uso',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './terminos-uso.component.html'
})
export class TerminosUsoComponent {
  constructor(private router: Router) {}

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
