import { Routes } from '@angular/router';
import { AuthGuard } from './Core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    canActivate: [AuthGuard],
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
    canActivate: [AuthGuard],
    loadComponent: () =>
      import('./Pages/cart.component/cart.component').then((m) => m.CartComponent),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
