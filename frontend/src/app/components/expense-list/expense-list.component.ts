import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ExpenseService } from '../../services/expense.service';
import { Expense } from '../../models/expense.model';
import { ExpenseListTableComponent } from './tables/expense-list-table.component';

type CategoryBreakdown = {
  category: string;
  total: number;
  count: number;
  percentage: number;
};

type monthlyBreakdown = { month: string; total: number };

@Component({
  selector: 'app-expense-list',
  standalone: true,
  imports: [CommonModule, RouterLink, ExpenseListTableComponent],
  templateUrl: './expense-list.component.html',
  styleUrls: ['./expense-list.component.css'],
})
export class ExpenseListComponent implements OnInit {
  expenses: Expense[] = [];
  loading = true;
  error = '';

  totalCount = 0;
  totalAmount = 0;
  averageAmount = 0;
  topCategory = '—';

  page = 1;
  pageSize = 10;
  totalPages = 0;

  search = '';
  selectedCategory = '';
  sortBy = 'date';
  sortDirection: 'asc' | 'desc' = 'desc';

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

  categoryBreakdown: CategoryBreakdown[] = [];
  monthlyBreakdown: monthlyBreakdown[] = [];

  constructor(private readonly expenseService: ExpenseService) {}

  ngOnInit(): void {
    this.loadExpenses();
  }

  loadExpenses(): void {
    this.loading = true;
    this.error = '';

    this.expenseService
      .getExpenses(
        this.page,
        this.pageSize,
        this.selectedCategory || undefined,
        this.search || undefined,
        this.sortBy,
        this.sortDirection,
      )
      .subscribe({
        next: (response) => {
          this.expenses = response.items;
          this.totalCount = response.totalCount;
          this.totalPages = response.totalPages;

          this.computeDashboardMetrics();
          this.loading = false;
        },
        error: (err) => {
          this.error = 'Failed to load expenses';
          this.loading = false;
          console.error(err);
        },
      });
  }

  private computeDashboardMetrics(): void {
    this.totalAmount = this.expenses.reduce(
      (sum, expense) => sum + expense.amount,
      0,
    );

    this.averageAmount =
      this.expenses.length > 0 ? this.totalAmount / this.expenses.length : 0;

    const categoryMap = new Map<string, { total: number; count: number }>();

    for (const expense of this.expenses) {
      const current = categoryMap.get(expense.category) ?? {
        total: 0,
        count: 0,
      };
      current.total += expense.amount;
      current.count += 1;
      categoryMap.set(expense.category, current);
    }

    this.categoryBreakdown = Array.from(categoryMap.entries())
      .map(([category, data]) => ({
        category,
        total: data.total,
        count: data.count,
        percentage:
          this.totalAmount > 0 ? (data.total / this.totalAmount) * 100 : 0,
      }))
      .sort((a, b) => b.total - a.total);

    this.topCategory =
      this.categoryBreakdown.length > 0
        ? this.categoryBreakdown[0].category
        : '—';

    const monthMap = new Map<string, number>();

    for (const expense of this.expenses) {
      const date = new Date(expense.date);
      const monthKey = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
      monthMap.set(monthKey, (monthMap.get(monthKey) ?? 0) + expense.amount);
    }

    this.monthlyBreakdown = Array.from(monthMap.entries())
      .map(([month, total]) => ({ month, total }))
      .sort((a, b) => a.month.localeCompare(b.month));
  }

  onDeleteExpense(id: string): void {
    this.error = '';

    if (confirm('Are you sure you want to delete this expense?')) {
      this.expenseService.deleteExpense(id).subscribe({
        next: () => {
          this.loadExpenses();
        },
        error: (err) => {
          this.error =
            err.status === 404
              ? 'Expense not found'
              : 'Failed to delete expense';
          console.error(err);
        },
      });
    }
  }

  applyFilters(): void {
    this.page = 1;
    this.loadExpenses();
  }

  clearFilters(): void {
    this.search = '';
    this.selectedCategory = '';
    this.sortBy = 'date';
    this.sortDirection = 'desc';
    this.page = 1;
    this.loadExpenses();
  }

  changePage(newPage: number): void {
    if (newPage < 1 || newPage > this.totalPages) {
      return;
    }

    this.page = newPage;
    this.loadExpenses();
  }

  changeSort(column: string): void {
    console.log('sort', this.sortBy, this.sortDirection);
    if (this.sortBy === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortDirection = 'asc';
    }

    this.page = 1;
    this.loadExpenses();
  }

  changePageSize(size: number) {
    this.pageSize = size;
    this.page = 1;
    this.loadExpenses();
  }
}
