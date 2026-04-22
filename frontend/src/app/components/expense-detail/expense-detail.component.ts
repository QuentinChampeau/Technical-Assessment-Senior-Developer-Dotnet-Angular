import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ExpenseService } from '../../services/expense.service';
import { Expense } from '../../models/expense.model';

@Component({
  selector: 'app-expense-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './expense-detail.component.html',
  styleUrls: ['./expense-detail.component.css']
})
export class ExpenseDetailComponent implements OnInit {
  expense: Expense | undefined;
  loading = true;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private expenseService: ExpenseService
  ) {}

  ngOnInit(): void {
    this.loadExpense();
  }

  loadExpense(): void {
    const companyId = this.route.snapshot.paramMap.get('id');
    if (!companyId) {
      this.error = 'Expense ID is required';
      this.loading = false;
      return;
    }

    this.expenseService.getExpense(companyId).subscribe({
      next: (expense) => {
        if (expense) {
          this.expense = expense;
        } else {
          this.error = 'Expense not found';
        }
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load expense details';
        this.loading = false;
        console.error(err);
      }
    });
  }

  deleteExpense(): void {
    if (!this.expense) return;

    if (confirm('Are you sure you want to delete this expense?')) {
      this.expenseService.deleteExpense(this.expense.company_id).subscribe({
        next: (success) => {
          if (success) {
            this.router.navigate(['/expenses']);
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
