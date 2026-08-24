export interface Product {
  id: string;
  name: string;
  category: string;
  price: number;
  stockQuantity: number;
  description?: string;
  imageUrl?: string;
}

export interface ProductQueryParams {
  category?: string;
  search?: string;
  inStockOnly?: boolean;
}
