import { CommonModule } from '@angular/common';
import { Component, OnInit, QueryList, ViewChildren } from '@angular/core';
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
  readonly cart$;
  readonly checkoutForm;

  @ViewChildren(CartItemComponent) private readonly cartItems!: QueryList<CartItemComponent>;

  loadError = '';
  itemError = '';
  checkoutError = '';
  checkoutResult: CheckoutResult | null = null;
  checkingOut = false;
  showPaymentForm = false;

  constructor(
    private readonly cartService: CartService,
    private readonly fb: FormBuilder,
    private readonly router: Router
  ) {
    this.cart$ = this.cartService.cart$;
    this.checkoutForm = this.fb.group({
      currency: ['USD', [Validators.required]],
      cardholderName: ['', [Validators.required]],
      cardNumber: ['', [Validators.required, Validators.pattern(/^[\d\s-]{13,23}$/)]],
      expiryMonth: [null as number | null, [Validators.required, Validators.min(1), Validators.max(12)]],
      expiryYear: [null as number | null, [Validators.required, Validators.min(new Date().getFullYear())]],
      cvv: ['', [Validators.required, Validators.pattern(/^\d{3,4}$/)]],
    });
  }

  ngOnInit(): void {
    this.cartService.loadCart().subscribe({
      error: () => (this.loadError = 'Could not load your cart.'),
    });
  }

  proceedToPayment(): void {
    this.showPaymentForm = true;
  }

  backToCart(): void {
    this.showPaymentForm = false;
    this.checkoutError = '';
  }

  fieldInvalid(name: string): boolean {
    const control = this.checkoutForm.get(name);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  onQuantityChange(productId: string, quantity: number): void {
    this.itemError = '';
    this.cartService.updateItem(productId, { quantity }).subscribe({
      error: (err) => {
        this.itemError =
          err?.error?.title ??
          (Array.isArray(err?.error) ? err.error[0]?.error : null) ??
          'Could not update quantity. Please try again.';
        this.cartItems.find((c) => c.item.productId === productId)?.resetDisplayedQuantity();
      },
    });
  }

  onRemove(productId: string): void {
    this.itemError = '';
    this.cartService.removeItem(productId).subscribe({
      error: () => (this.itemError = 'Could not remove item. Please try again.'),
    });
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
        cardNumber: cardNumber!.replace(/[\s-]/g, ''),
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
        error: (err) => {
          this.checkoutError =
            err?.error?.title ??
            (Array.isArray(err?.error) ? err.error[0]?.error : null) ??
            'Checkout failed. Please check your payment details and try again.';
        },
      });
  }

  continueShopping(): void {
    this.router.navigateByUrl('/');
  }
}
