import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PaginationComponent } from '../../../common/components/pagination/pagination.component';
import { Expense } from '../../../models/expense.model';

@Component({
  selector: 'app-expense-list-table',
  standalone: true,
  imports: [CommonModule, RouterLink, PaginationComponent],
  templateUrl: './expense-list-table.component.html',
})
export class ExpenseListTableComponent {
  expenses = input<Expense[]>([]);

  page = input(1);
  pageSize = input(10);
  totalCount = input(0);

  sortBy = input('date');
  sortDirection = input<'asc' | 'desc'>('desc');

  sortChange = output<string>();
  pageChange = output<number>();
  pageSizeChange = output<number>();
  delete = output<string>();

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
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
}
