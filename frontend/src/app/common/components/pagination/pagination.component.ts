import { CommonModule } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';

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
  pageSizeOptions = input<number[]>([5, 10, 20, 50]);

  pageChange = output<number>();
  pageSizeChange = output<number>();

  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize())),
  );

  startItem = computed(() => {
    if (this.totalCount() === 0) {
      return 0;
    }

    return (this.page() - 1) * this.pageSize() + 1;
  });

  endItem = computed(() =>
    Math.min(this.page() * this.pageSize(), this.totalCount()),
  );

  previous(): void {
    if (this.page() > 1) {
      this.pageChange.emit(this.page() - 1);
    }
  }

  next(): void {
    if (this.page() < this.totalPages()) {
      this.pageChange.emit(this.page() + 1);
    }
  }

  onPageSizeChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.pageSizeChange.emit(value);
  }
}
