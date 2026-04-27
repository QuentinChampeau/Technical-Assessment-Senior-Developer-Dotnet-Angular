import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ExpenseService } from '../../services/expense.service';

@Component({
  selector: 'app-expense-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './expense-form.component.html',
  styleUrls: ['./expense-form.component.css'],
})
export class ExpenseFormComponent implements OnInit {
  expenseForm!: FormGroup;
  loading = false;
  error = '';
  submitted = false;

  expenseId: string | null = null;
  isEditMode = false;

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

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly expenseService: ExpenseService,
  ) {}

  ngOnInit(): void {
    this.expenseId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.expenseId;

    this.initForm();

    if (this.isEditMode && this.expenseId) {
      this.loadExpense(this.expenseId);
    }
  }

  private initForm(): void {
    this.expenseForm = this.fb.group({
      amount: [null, [Validators.required, Validators.min(0.01)]],
      category: ['', Validators.required],
      date: [this.formatDateForInput(new Date()), Validators.required],
      description: [
        '',
        [
          Validators.required,
          Validators.minLength(5),
          Validators.maxLength(200),
        ],
      ],
    });
  }

  private loadExpense(id: string): void {
    this.loading = true;

    this.expenseService.getExpense(id).subscribe({
      next: (expense) => {
        this.expenseForm.patchValue({
          amount: expense.amount,
          category: expense.category,
          date: this.formatDateForInput(new Date(expense.date)),
          description: expense.description,
        });

        this.loading = false;
      },
      error: (err) => {
        this.error =
          err.status === 404 ? 'Expense not found' : 'Failed to load expense';
        this.loading = false;
        console.error(err);
      },
    });
  }

  onSubmit(): void {
    this.submitted = true;
    this.error = '';

    if (this.expenseForm.invalid) {
      return;
    }

    this.loading = true;

    const formValue = this.expenseForm.value;

    const request = {
      amount: Number(formValue.amount),
      category: formValue.category,
      date: new Date(formValue.date).toISOString(),
      description: formValue.description.trim(),
    };

    const save$ =
      this.isEditMode && this.expenseId
        ? this.expenseService.updateExpense(this.expenseId, request)
        : this.expenseService.createExpense(request);

    save$.subscribe({
      next: (expense) => {
        this.router.navigate(['/expenses', expense.id]);
      },
      error: (err) => {
        this.error = this.isEditMode
          ? 'Failed to update expense'
          : 'Failed to create expense';
        this.loading = false;
        console.error(err);
      },
    });
  }

  private formatDateForInput(date: Date): string {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');

    return `${year}-${month}-${day}`;
  }
}
