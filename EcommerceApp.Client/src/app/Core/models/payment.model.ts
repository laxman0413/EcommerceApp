export interface ChargeRequestDto {
  amount: number;
  currency?: string;
  cardNumber?: string;
  cardholderName?: string;
  expiryMonth: number;
  expiryYear: number;
  cvv?: string;
}

// Charge response shape isn't documented; treated as loosely-typed.
export interface ChargeResult {
  success?: boolean;
  transactionId?: string;
  message?: string;
}
