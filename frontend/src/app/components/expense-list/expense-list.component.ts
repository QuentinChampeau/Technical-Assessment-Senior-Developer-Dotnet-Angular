import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ExpenseService } from '../../services/expense.service';
import { Expense } from '../../models/expense.model';

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
  imports: [CommonModule, RouterLink],
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

  categoryBreakdown: CategoryBreakdown[] = [];
  monthlyBreakdown: monthlyBreakdown[] = [];

  constructor(private readonly expenseService: ExpenseService) {}

  ngOnInit(): void {
    this.loadExpenses();
  }

  loadExpenses(): void {
    this.loading = true;
    this.error = '';

    this.expenseService.getExpenses().subscribe({
      next: (response) => {
        this.expenses = response.items;
        this.totalCount = response.totalCount;

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

  deleteExpense(id: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();

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

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
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
}
