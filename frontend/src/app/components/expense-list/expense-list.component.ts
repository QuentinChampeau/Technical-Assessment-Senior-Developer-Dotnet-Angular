import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ExpenseService } from '../../services/expense.service';
import { Expense } from '../../models/expense.model';

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

  constructor(private readonly expenseService: ExpenseService) {}

  ngOnInit(): void {
    this.loadExpenses();
  }

  loadExpenses(): void {
    this.loading = true;
    this.error = '';

    this.expenseService.getExpenses().subscribe({
      next: (expenses) => {
        this.expenses = expenses.items;
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
}
