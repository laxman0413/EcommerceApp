import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Product, ProductQueryParams } from '../models/product.model';

@Injectable({
  providedIn: 'root',
})
export class ProductService {
  private readonly apiUrl = `${environment.apiUrl}/Products`;

  constructor(private readonly http: HttpClient) {}

  getProducts(query?: ProductQueryParams): Observable<Product[]> {
    let params = new HttpParams();
    if (query?.category) {
      params = params.set('category', query.category);
    }
    if (query?.search) {
      params = params.set('search', query.search);
    }
    if (query?.inStockOnly) {
      params = params.set('inStockOnly', query.inStockOnly);
    }
    return this.http.get<Product[]>(this.apiUrl, { params });
  }

  getProduct(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }
}
