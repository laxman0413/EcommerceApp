import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Product } from '../../Core/models/product.model';

@Component({
  selector: 'app-product',
  imports: [CommonModule, RouterLink],
  templateUrl: './product.component.html',
  styleUrl: './product.component.css',
})
export class ProductComponent {
  @Input() product!: Product;
  @Input() inCart = false;
  @Output() addToCart = new EventEmitter<string>();
  @Output() removeFromCart = new EventEmitter<string>();

  onAddToCart(): void {
    this.addToCart.emit(this.product.id);
  }

  onRemoveFromCart(): void {
    this.removeFromCart.emit(this.product.id);
  }
}
