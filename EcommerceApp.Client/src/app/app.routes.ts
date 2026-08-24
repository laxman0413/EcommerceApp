import { Routes } from '@angular/router';
import { authGuard } from './Core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./Pages/home.component/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./Pages/login.component/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./Pages/register.component/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'cart',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./Pages/cart.component/cart.component').then((m) => m.CartComponent),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
