import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-particulares',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './particulares.component.html'
})
export class ParticularesComponent {
  constructor(private router: Router) {}

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
