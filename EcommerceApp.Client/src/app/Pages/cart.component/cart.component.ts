import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CartService } from '../../Core/services/cart.service';
import { CheckoutResult } from '../../Core/models/cart.model';
import { CartItemComponent } from '../../Shared/cart-item.component/cart-item.component';

@Component({
  selector: 'app-cart',
  imports: [CommonModule, ReactiveFormsModule, CartItemComponent],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.css',
})
export class CartComponent implements OnInit {
  private readonly cartService = inject(CartService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  readonly cart$ = this.cartService.cart$;

  readonly checkoutForm = this.fb.group({
    currency: ['USD', [Validators.required]],
    cardholderName: ['', [Validators.required]],
    cardNumber: ['', [Validators.required, Validators.pattern(/^\d{13,19}$/)]],
    expiryMonth: [null as number | null, [Validators.required, Validators.min(1), Validators.max(12)]],
    expiryYear: [null as number | null, [Validators.required, Validators.min(new Date().getFullYear())]],
    cvv: ['', [Validators.required, Validators.pattern(/^\d{3,4}$/)]],
  });

  loadError = '';
  checkoutError = '';
  checkoutResult: CheckoutResult | null = null;
  checkingOut = false;

  ngOnInit(): void {
    this.cartService.loadCart().subscribe({
      error: () => (this.loadError = 'Could not load your cart.'),
    });
  }

  onQuantityChange(productId: string, quantity: number): void {
    this.cartService.updateItem(productId, { quantity }).subscribe();
  }

  onRemove(productId: string): void {
    this.cartService.removeItem(productId).subscribe();
  }

  onCheckout(): void {
    if (this.checkoutForm.invalid) {
      this.checkoutForm.markAllAsTouched();
      return;
    }

    this.checkingOut = true;
    this.checkoutError = '';
    this.checkoutResult = null;

    const { currency, cardholderName, cardNumber, expiryMonth, expiryYear, cvv } =
      this.checkoutForm.getRawValue();

    this.cartService
      .checkout({
        currency: currency!,
        cardholderName: cardholderName!,
        cardNumber: cardNumber!,
        expiryMonth: expiryMonth!,
        expiryYear: expiryYear!,
        cvv: cvv!,
      })
      .pipe(finalize(() => (this.checkingOut = false)))
      .subscribe({
        next: (result) => {
          this.checkoutResult = result;
          this.checkoutForm.reset({ currency: 'USD' });
        },
        error: () => {
          this.checkoutError = 'Checkout failed. Please check your payment details and try again.';
        },
      });
  }

  continueShopping(): void {
    this.router.navigateByUrl('/');
  }
}
