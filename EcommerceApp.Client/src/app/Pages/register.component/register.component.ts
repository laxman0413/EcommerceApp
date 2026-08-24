import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, switchMap } from 'rxjs/operators';
import { AuthService } from '../../Core/services/auth.service';

@Component({
  selector: 'app-register',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly form = this.fb.group({
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  loading = false;
  errorMessage = '';

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const { firstName, lastName, email, password } = this.form.getRawValue();
    this.authService
      .register({ firstName: firstName!, lastName: lastName!, email: email!, password: password! })
      .pipe(
        switchMap(() => this.authService.login({ email: email!, password: password! })),
        finalize(() => (this.loading = false))
      )
      .subscribe({
        next: () => this.router.navigateByUrl('/'),
        error: () => {
          this.errorMessage = 'Registration failed. The email may already be in use.';
        },
      });
  }
}
