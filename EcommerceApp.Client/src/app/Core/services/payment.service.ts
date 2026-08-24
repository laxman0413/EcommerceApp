import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChargeRequestDto, ChargeResult } from '../models/payment.model';

@Injectable({
  providedIn: 'root',
})
export class PaymentService {
  private readonly apiUrl = `${environment.apiUrl}/Payments`;

  constructor(private readonly http: HttpClient) {}

  charge(dto: ChargeRequestDto): Observable<ChargeResult> {
    return this.http.post<ChargeResult>(`${this.apiUrl}/charge`, dto);
  }
}
