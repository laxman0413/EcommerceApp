import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  AddCartItemDto,
  Cart,
  CheckoutRequestDto,
  CheckoutResult,
  UpdateCartItemDto,
} from '../models/cart.model';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private readonly apiUrl = `${environment.apiUrl}/Cart`;

  private readonly cartSubject = new BehaviorSubject<Cart | null>(null);
  readonly cart$ = this.cartSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  loadCart(): Observable<Cart> {
    return this.http.get<Cart>(this.apiUrl).pipe(tap((cart) => this.cartSubject.next(cart)));
  }

  addItem(dto: AddCartItemDto): Observable<Cart> {
    return this.http
      .post<Cart>(`${this.apiUrl}/items`, dto)
      .pipe(tap((cart) => this.cartSubject.next(cart)));
  }

  updateItem(productId: string, dto: UpdateCartItemDto): Observable<Cart> {
    return this.http
      .put<Cart>(`${this.apiUrl}/items/${productId}`, dto)
      .pipe(tap((cart) => this.cartSubject.next(cart)));
  }

  removeItem(productId: string): Observable<Cart> {
    return this.http
      .delete<Cart>(`${this.apiUrl}/items/${productId}`)
      .pipe(tap((cart) => this.cartSubject.next(cart)));
  }

  checkout(dto: CheckoutRequestDto): Observable<CheckoutResult> {
    return this.http
      .post<CheckoutResult>(`${this.apiUrl}/checkout`, dto)
      .pipe(tap(() => this.cartSubject.next(null)));
  }

  clearLocal(): void {
    this.cartSubject.next(null);
  }
}
