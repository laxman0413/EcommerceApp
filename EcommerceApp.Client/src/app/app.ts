import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
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
  readonly currentUser$;
  readonly cartItemCount$;

  constructor(
    private readonly authService: AuthService,
    private readonly cartService: CartService,
    private readonly router: Router
  ) {
    this.currentUser$ = this.authService.currentUser$;
    this.cartItemCount$ = this.cartService.cart$.pipe(
      map((cart) => cart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0)
    );
  }

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
