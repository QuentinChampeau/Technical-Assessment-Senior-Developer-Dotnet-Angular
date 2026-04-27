import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ExpenseService } from '@services/expense.service';
import { Expense } from '@models/expense.model';
import { AuditEntry } from '@models/audit-entry.model';

@Component({
  selector: 'app-expense-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './expense-detail.component.html',
  styleUrls: ['./expense-detail.component.css'],
})
export class ExpenseDetailComponent implements OnInit {
  expense: Expense | undefined;
  history: AuditEntry[] = [];

  expandedHistoryIds = new Set<number>();

  loading = true;
  historyLoading = false;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private expenseService: ExpenseService,
  ) {}

  ngOnInit(): void {
    this.loadExpense();
  }

  loadExpense(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error = 'Expense ID is required';
      this.loading = false;
      return;
    }

    this.expenseService.getExpense(id).subscribe({
      next: (expense) => {
        this.expense = expense;
        this.loading = false;
        this.loadChangeHistory(id);
      },
      error: (err) => {
        this.error =
          err.status === 404
            ? 'Expense not found'
            : 'Failed to load expense details';
        this.loading = false;
        console.error(err);
      },
    });
  }

  loadChangeHistory(id: string): void {
    this.historyLoading = true;

    this.expenseService.getHistory(id).subscribe({
      next: (history) => {
        this.history = history;
        this.historyLoading = false;
      },
      error: (err) => {
        this.historyLoading = false;
        console.error(err);
      },
    });
  }

  deleteExpense(): void {
    if (!this.expense) return;

    if (confirm('Are you sure you want to delete this expense?')) {
      this.expenseService.deleteExpense(this.expense.id).subscribe({
        next: () => {
          this.router.navigate(['/expenses']);
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
  toggleHistoryDetails(id: number): void {
    if (this.expandedHistoryIds.has(id)) {
      this.expandedHistoryIds.delete(id);
      return;
    }

    this.expandedHistoryIds.add(id);
  }

  isHistoryExpanded(id: number): boolean {
    return this.expandedHistoryIds.has(id);
  }

  formatDate(dateString: Date | string): string {
    return new Date(dateString).toLocaleString();
  }

  formatChanges(changesJson: string): string {
    try {
      return JSON.stringify(JSON.parse(changesJson), null, 2);
    } catch {
      return changesJson;
    }
  }

  getChangesPreview(changesJson: string): string {
    try {
      const parsed = JSON.parse(changesJson);
      const keys = Object.keys(parsed);

      if (keys.length === 0) {
        return 'No field changes';
      }

      return keys.slice(0, 3).join(', ') + (keys.length > 3 ? '…' : '');
    } catch {
      return 'Raw changes';
    }
  }
}
