import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pagination.component.html',
})
export class PaginationComponent {
  page = input(1);
  pageSize = input(10);
  totalCount = input(0);
  pageSizeOptions = input([5, 10, 20, 50]);

  pageChange = output<number>();
  pageSizeChange = output<number>();

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize()));
  }

  get startItem(): number {
    if (this.totalCount() === 0) return 0;
    return (this.page() - 1) * this.pageSize() + 1;
  }

  get endItem(): number {
    return Math.min(this.page() * this.pageSize(), this.totalCount());
  }

  previous(): void {
    if (this.page() > 1) {
      this.pageChange.emit(this.page() - 1);
    }
  }

  next(): void {
    if (this.page() < this.totalPages) {
      this.pageChange.emit(this.page() + 1);
    }
  }

  changePageSize(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.pageSizeChange.emit(value);
  }
}
