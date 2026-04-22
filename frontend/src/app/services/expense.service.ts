import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { Expense } from '../models/expense.model';
import { v4 as uuidv4 } from 'uuid';

@Injectable({
  providedIn: 'root'
})
export class ExpenseService {
  private readonly STORAGE_KEY = 'expenses';

  constructor() {
    // Initialize with some sample data if empty
    if (!localStorage.getItem(this.STORAGE_KEY)) {
      const sampleExpenses: Expense[] = [
        {
          company_id: uuidv4(),
          amount: 42.24,
          currency: 'USD',
          category: 'Office Supplies',
          date: new Date().toISOString(),
          description: 'This is an expense of 42.24 dollars',
          employee_id: uuidv4(),
          created_at: new Date().toISOString(),
          updated_at: new Date().toISOString()
        },
        {
          company_id: uuidv4(),
          amount: 120.50,
          currency: 'EUR',
          category: 'Travel',
          date: new Date().toISOString(),
          description: 'Business trip to Paris',
          employee_id: uuidv4(),
          created_at: new Date().toISOString(),
          updated_at: new Date().toISOString()
        }
      ];
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(sampleExpenses));
    }
  }

  // Get all expenses
  getExpenses(): Observable<Expense[]> {
    const expenses = JSON.parse(localStorage.getItem(this.STORAGE_KEY) || '[]');
    return of(expenses);
  }

  // Get expense by company_id
  getExpense(companyId: string): Observable<Expense | undefined> {
    const expenses = JSON.parse(localStorage.getItem(this.STORAGE_KEY) || '[]');
    const expense = expenses.find((e: Expense) => e.company_id === companyId);
    return of(expense);
  }

  // Create a new expense
  createExpense(expense: Omit<Expense, 'company_id' | 'created_at' | 'updated_at'>): Observable<Expense> {
    const expenses = JSON.parse(localStorage.getItem(this.STORAGE_KEY) || '[]');
    const now = new Date().toISOString();

    const newExpense: Expense = {
      ...expense,
      company_id: uuidv4(),
      created_at: now,
      updated_at: now
    };

    expenses.push(newExpense);
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(expenses));

    return of(newExpense);
  }

  // Update an existing expense
  updateExpense(expense: Expense): Observable<Expense> {
    const expenses = JSON.parse(localStorage.getItem(this.STORAGE_KEY) || '[]');
    const index = expenses.findIndex((e: Expense) => e.company_id === expense.company_id);

    if (index !== -1) {
      const updatedExpense = {
        ...expense,
        updated_at: new Date().toISOString()
      };

      expenses[index] = updatedExpense;
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(expenses));

      return of(updatedExpense);
    }

    throw new Error('Expense not found');
  }

  // Delete an expense
  deleteExpense(companyId: string): Observable<boolean> {
    const expenses = JSON.parse(localStorage.getItem(this.STORAGE_KEY) || '[]');
    const filteredExpenses = expenses.filter((e: Expense) => e.company_id !== companyId);

    if (filteredExpenses.length !== expenses.length) {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(filteredExpenses));
      return of(true);
    }

    return of(false);
  }
}
