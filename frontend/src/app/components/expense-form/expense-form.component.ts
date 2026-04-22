import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ExpenseService } from '../../services/expense.service';

@Component({
  selector: 'app-expense-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './expense-form.component.html',
  styleUrls: ['./expense-form.component.css']
})
export class ExpenseFormComponent implements OnInit {
  expenseForm!: FormGroup;
  loading = false;
  error = '';
  submitted = false;

  // Currency options
  currencies = ['USD', 'EUR', 'GBP', 'JPY', 'CAD', 'AUD'];

  // Category options
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
    'Other'
  ];

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private expenseService: ExpenseService
  ) {}

  ngOnInit(): void {
    this.initForm();
  }

  initForm(): void {
    this.expenseForm = this.fb.group({
      amount: ['', [Validators.required, Validators.min(0.01)]],
      currency: ['USD', Validators.required],
      category: ['', Validators.required],
      date: [this.formatDateForInput(new Date()), Validators.required],
      description: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]],
      employee_id: ['', Validators.required]
    });
  }

  onSubmit(): void {
    this.submitted = true;

    if (this.expenseForm.invalid) {
      return;
    }

    this.loading = true;

    const formValue = this.expenseForm.value;

    // Convert date string to ISO format
    const expense = {
      ...formValue,
      date: new Date(formValue.date).toISOString()
    };

    this.expenseService.createExpense(expense).subscribe({
      next: () => {
        this.router.navigate(['/expenses']);
      },
      error: (err) => {
        this.error = 'Failed to create expense';
        this.loading = false;
        console.error(err);
      }
    });
  }

  // Helper method to format date for the input field
  private formatDateForInput(date: Date): string {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
