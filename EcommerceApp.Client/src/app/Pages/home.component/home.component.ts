import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, map, switchMap, tap } from 'rxjs/operators';
import { CartService } from '../../Core/services/cart.service';
import { ProductService } from '../../Core/services/product.service';
import { Product, ProductQueryParams } from '../../Core/models/product.model';
import { ProductComponent } from '../../Shared/product.component/product.component';

interface ProductQuery {
  filters: ProductQueryParams;
  page: number;
}

const PAGE_SIZE = 12;

@Component({
  selector: 'app-home',
  imports: [CommonModule, ReactiveFormsModule, ProductComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent implements OnInit {
  readonly filterForm;
  readonly cartProductIds$;

  private readonly query$ = new BehaviorSubject<ProductQuery>({ filters: {}, page: 1 });

  products$!: Observable<Product[]>;
  statusMessage = '';
  loadError = '';
  totalCount = 0;
  totalPages = 0;

  constructor(
    private readonly productService: ProductService,
    private readonly cartService: CartService,
    private readonly fb: FormBuilder
  ) {
    this.filterForm = this.fb.group({
      search: [''],
      category: [''],
      inStockOnly: [false],
    });
    this.cartProductIds$ = this.cartService.cart$.pipe(
      map((cart) => new Set(cart?.items.map((item) => item.productId) ?? []))
    );
  }

  get currentPage(): number {
    return this.query$.value.page;
  }

  ngOnInit(): void {
    this.filterForm.valueChanges
      .pipe(
        debounceTime(300),
        map((filters) => ({
          search: filters.search || undefined,
          category: filters.category || undefined,
          inStockOnly: filters.inStockOnly || undefined,
        })),
        distinctUntilChanged((prev, curr) => JSON.stringify(prev) === JSON.stringify(curr))
      )
      .subscribe((filters) => this.query$.next({ filters, page: 1 }));

    this.products$ = this.query$.pipe(
      switchMap(({ filters, page }) =>
        this.productService.getProducts({ ...filters, page, pageSize: PAGE_SIZE }).pipe(
          tap((result) => {
            this.loadError = '';
            this.totalCount = result.totalCount;
            this.totalPages = result.totalPages;
          }),
          map((result) => result.items),
          catchError(() => {
            this.loadError = 'Could not load products. Please try again.';
            this.totalCount = 0;
            this.totalPages = 0;
            return of<Product[]>([]);
          })
        )
      )
    );
  }

  goToPage(page: number): void {
    const current = this.query$.value;
    if (page < 1 || page > this.totalPages || page === current.page) {
      return;
    }
    this.query$.next({ filters: current.filters, page });
  }

  onAddToCart(productId: string): void {
    this.cartService.addItem({ productId, quantity: 1 }).subscribe({
      next: () => this.showStatus('Added to cart.'),
      error: () => this.showStatus('Could not add item to cart. Please sign in and try again.'),
    });
  }

  onRemoveFromCart(productId: string): void {
    this.cartService.removeItem(productId).subscribe({
      next: () => this.showStatus('Removed from cart.'),
      error: () => this.showStatus('Could not remove item from cart. Please try again.'),
    });
  }

  private showStatus(message: string): void {
    this.statusMessage = message;
    setTimeout(() => (this.statusMessage = ''), 3000);
  }
}
