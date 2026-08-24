import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { map } from 'rxjs/operators';
import { AuthService } from './Core/services/auth.service';
import { CartService } from './Core/services/cart.service';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly cartService = inject(CartService);
  private readonly router = inject(Router);

  readonly currentUser$ = this.authService.currentUser$;
  readonly cartItemCount$ = this.cartService.cart$.pipe(
    map((cart) => cart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0)
  );

  ngOnInit(): void {
    this.authService.currentUser$.subscribe((user) => {
      if (user) {
        this.cartService.loadCart().subscribe();
      } else {
        this.cartService.clearLocal();
      }
    });
  }

  logout(): void {
    this.authService.logout().subscribe(() => this.router.navigateByUrl('/login'));
  }
}
