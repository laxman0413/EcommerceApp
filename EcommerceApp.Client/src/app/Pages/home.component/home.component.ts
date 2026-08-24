import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { debounceTime, distinctUntilChanged, startWith, switchMap } from 'rxjs/operators';
import { CartService } from '../../Core/services/cart.service';
import { ProductService } from '../../Core/services/product.service';
import { Product } from '../../Core/models/product.model';
import { ProductComponent } from '../../Shared/product.component/product.component';

@Component({
  selector: 'app-home',
  imports: [CommonModule, ReactiveFormsModule, ProductComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);
  private readonly fb = inject(FormBuilder);

  readonly filterForm = this.fb.group({
    search: [''],
    category: [''],
    inStockOnly: [false],
  });

  products$!: Observable<Product[]>;
  statusMessage = '';

  ngOnInit(): void {
    this.products$ = this.filterForm.valueChanges.pipe(
      startWith(this.filterForm.value),
      debounceTime(300),
      distinctUntilChanged((prev, curr) => JSON.stringify(prev) === JSON.stringify(curr)),
      switchMap((filters) =>
        this.productService.getProducts({
          search: filters.search || undefined,
          category: filters.category || undefined,
          inStockOnly: filters.inStockOnly || undefined,
        })
      )
    );
  }

  onAddToCart(productId: string): void {
    this.cartService.addItem({ productId, quantity: 1 }).subscribe({
      next: () => this.showStatus('Added to cart.'),
      error: () => this.showStatus('Could not add item to cart. Please sign in and try again.'),
    });
  }

  private showStatus(message: string): void {
    this.statusMessage = message;
    setTimeout(() => (this.statusMessage = ''), 3000);
  }
}
