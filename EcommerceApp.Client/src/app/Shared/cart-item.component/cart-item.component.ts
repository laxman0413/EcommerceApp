import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CartItem } from '../../Core/models/cart.model';

@Component({
  selector: 'app-cart-item',
  imports: [CommonModule],
  templateUrl: './cart-item.component.html',
  styleUrl: './cart-item.component.css',
})
export class CartItemComponent {
  @Input() item!: CartItem;
  @Output() quantityChange = new EventEmitter<number>();
  @Output() remove = new EventEmitter<void>();

  onQuantityInput(value: string): void {
    const quantity = Number(value);
    if (Number.isFinite(quantity) && quantity > 0) {
      this.quantityChange.emit(quantity);
    }
  }

  onRemove(): void {
    this.remove.emit();
  }
}
