import { CommonModule } from '@angular/common';
import { Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { CartItem } from '../../Core/models/cart.model';

const MAX_QUANTITY = 4;

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

  @ViewChild('qtyInput') private readonly qtyInput?: ElementRef<HTMLSelectElement>;


  get quantityOptions(): number[] {
    const max = Math.max(MAX_QUANTITY, this.item.quantity);
    return Array.from({ length: max }, (_, i) => i + 1);
  }

  onQuantityInput(value: string): void {
    const quantity = Number(value);
    if (Number.isFinite(quantity) && quantity > 0) {
      this.quantityChange.emit(quantity);
    } else {
      this.resetDisplayedQuantity();
    }
  }

  resetDisplayedQuantity(): void {
    if (this.qtyInput) {
      this.qtyInput.nativeElement.value = String(this.item.quantity);
    }
  }

  onRemove(): void {
    this.remove.emit();
  }
}
