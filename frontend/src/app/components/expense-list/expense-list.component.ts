import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ExpenseService } from '../../services/expense.service';
import { Expense } from '../../models/expense.model';
import { ExpenseListTableComponent } from './tables/expense-list-table.component';
import { FormsModule } from '@angular/forms';
import { ExpenseDashboardChartsComponent } from './charts/expense-dashboard-charts.component';

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
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    ExpenseListTableComponent,
    ExpenseDashboardChartsComponent,
  ],
  templateUrl: './expense-list.component.html',
  styleUrls: ['./expense-list.component.css'],
})
export class ExpenseListComponent implements OnInit {
  dashboardExpenses: Expense[] = [];
  expenses: Expense[] = [];

  loadingDashboard = true;
  loadingTable = true;
  error = '';

  totalCount = 0;
  totalAmount = 0;
  averageAmount = 0;
  topCategory = '—';

  tableTotalCount = 0;
  tableTotalPages = 0;

  page = 1;
  pageSize = 10;
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
    this.loadDashboardExpenses();
    this.loadTableExpenses();
  }

  loadDashboardExpenses(): void {
    this.loadingDashboard = true;
    this.error = '';

    this.expenseService.getExpenses(1, 1000).subscribe({
      next: (response) => {
        this.dashboardExpenses = response.items;
        this.computeDashboardMetrics();
        this.loadingDashboard = false;
      },
      error: (err) => {
        this.error = 'Failed to load dashboard data';
        this.loadingDashboard = false;
        console.error(err);
      },
    });
  }

  loadTableExpenses(): void {
    this.loadingTable = true;
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
          this.tableTotalCount = response.totalCount;
          this.tableTotalPages = response.totalPages;
          this.loadingTable = false;
        },
        error: (err) => {
          this.error = 'Failed to load expenses';
          this.loadingTable = false;
          console.error(err);
        },
      });
  }

  onFiltersChange(filters: { search: string; category: string }): void {
    this.search = filters.search;
    this.selectedCategory = filters.category;
    this.page = 1;
    this.loadTableExpenses();
  }

  onSortChange(column: string): void {
    if (this.sortBy === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortDirection = 'asc';
    }

    this.page = 1;
    this.loadTableExpenses();
  }

  onPageChange(page: number): void {
    if (page < 1 || page > this.tableTotalPages) {
      return;
    }

    this.page = page;
    this.loadTableExpenses();
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.page = 1;
    this.loadTableExpenses();
  }

  onDeleteExpense(id: string): void {
    this.error = '';

    if (!confirm('Are you sure you want to delete this expense?')) {
      return;
    }

    this.expenseService.deleteExpense(id).subscribe({
      next: () => {
        this.loadDashboardExpenses();
        this.loadTableExpenses();
      },
      error: (err) => {
        this.error =
          err.status === 404 ? 'Expense not found' : 'Failed to delete expense';
        console.error(err);
      },
    });
  }

  private computeDashboardMetrics(): void {
    this.totalCount = this.dashboardExpenses.length;

    this.totalAmount = this.dashboardExpenses.reduce(
      (sum, expense) => sum + expense.amount,
      0,
    );

    this.averageAmount =
      this.dashboardExpenses.length > 0
        ? this.totalAmount / this.dashboardExpenses.length
        : 0;

    const categoryMap = new Map<string, { total: number; count: number }>();

    for (const expense of this.dashboardExpenses) {
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

    for (const expense of this.dashboardExpenses) {
      const date = new Date(expense.date);
      const monthKey = `${date.getFullYear()}-${String(
        date.getMonth() + 1,
      ).padStart(2, '0')}`;

      monthMap.set(monthKey, (monthMap.get(monthKey) ?? 0) + expense.amount);
    }

    this.monthlyBreakdown = Array.from(monthMap.entries())
      .map(([month, total]) => ({ month, total }))
      .sort((a, b) => a.month.localeCompare(b.month));
  }
}
