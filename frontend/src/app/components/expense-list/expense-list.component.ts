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
  styleUrls: ['./expense-list.component.css']
})
export class ExpenseListComponent implements OnInit {
  expenses: Expense[] = [];
  loading = true;
  error = '';

  constructor(private expenseService: ExpenseService) {}

  ngOnInit(): void {
    this.loadExpenses();
  }

  loadExpenses(): void {
    this.loading = true;
    this.expenseService.getExpenses().subscribe({
      next: (expenses) => {
        this.expenses = expenses;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load expenses';
        this.loading = false;
        console.error(err);
      }
    });
  }

  deleteExpense(companyId: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    if (confirm('Are you sure you want to delete this expense?')) {
      this.expenseService.deleteExpense(companyId).subscribe({
        next: (success) => {
          if (success) {
            this.loadExpenses();
          } else {
            this.error = 'Failed to delete expense';
          }
        },
        error: (err) => {
          this.error = 'Failed to delete expense';
          console.error(err);
        }
      });
    }
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }
}
