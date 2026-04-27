import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PaginationComponent } from '@common/components/pagination/pagination.component';
import { Expense } from '@models/expense.model';

@Component({
  selector: 'app-expense-list-table',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PaginationComponent],
  templateUrl: './expense-list-table.component.html',
})
export class ExpenseListTableComponent {
  expenses = input<Expense[]>([]);
  loading = input(false);

  page = input(1);
  pageSize = input(10);
  totalCount = input(0);

  sortBy = input('date');
  sortDirection = input<'asc' | 'desc'>('desc');

  filtersChange = output<{ search: string; category: string }>();
  sortChange = output<string>();
  pageChange = output<number>();
  pageSizeChange = output<number>();
  delete = output<string>();

  search = '';
  selectedCategory = '';

  categories = [
    'Office Supplies',
    'Travel',
    'Meals',
    'Entertainment',
    'Transportation',
    'Accommodation',
    'Software',
    'Hardware',
    'Marketing',
    'Other',
  ];

  applyFilters(): void {
    this.filtersChange.emit({
      search: this.search.trim(),
      category: this.selectedCategory,
    });
  }

  clearFilters(): void {
    this.search = '';
    this.selectedCategory = '';

    this.filtersChange.emit({
      search: '',
      category: '',
    });
  }

  onSort(column: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.sortChange.emit(column);
  }

  onDelete(id: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.delete.emit(id);
  }

  getSortIcon(column: string): string {
    if (this.sortBy() !== column) {
      return '↕';
    }

    return this.sortDirection() === 'asc' ? '↑' : '↓';
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }
}
