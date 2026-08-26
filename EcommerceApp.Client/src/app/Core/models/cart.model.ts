export interface AddCartItemDto {
  productId: string;
  quantity: number;
}

export interface UpdateCartItemDto {
  quantity: number;
}

// GET /api/Cart response shape isn't documented; assumed from the request DTOs.
export interface CartItem {
  productId: string;
  productName?: string;
  imageUrl?: string;
  unitPrice?: number;
  quantity: number;
  lineTotal?: number;
}

export interface Cart {
  id?: string;
  items: CartItem[];
  totalAmount?: number;
}

export interface CheckoutRequestDto {
  currency?: string;
  cardNumber?: string;
  cardholderName?: string;
  expiryMonth: number;
  expiryYear: number;
  cvv?: string;
}

export interface CheckoutResult {
  id: string;
  status: string;
  amount: number;
  currency: string;
  createdAt: string;
  gatewayReference?: string;
}
