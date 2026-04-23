import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateExpenseRequest, Expense } from '../models/expense.model';

interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root',
})
export class ExpenseService {
  private readonly apiUrl = 'https://localhost:7058/expenses';

  constructor(private readonly http: HttpClient) {}

  getExpenses(
    page: number = 1,
    pageSize: number = 10,
    category?: string,
    search?: string,
  ): Observable<PagedResponse<Expense>> {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (category) {
      params.append('category', category);
    }

    if (search) {
      params.append('search', search);
    }

    return this.http.get<PagedResponse<Expense>>(
      `${this.apiUrl}?${params.toString()}`,
    );
  }

  getExpense(id: string): Observable<Expense> {
    return this.http.get<Expense>(`${this.apiUrl}/${id}`);
  }

  createExpense(expense: CreateExpenseRequest): Observable<Expense> {
    return this.http.post<Expense>(this.apiUrl, expense);
  }
}
