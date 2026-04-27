import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { ExpenseService } from './expense.service';
import {
  CreateExpenseRequest,
  Expense,
  PagedResponse,
} from '../models/expense.model';

describe('ExpenseService', () => {
  let service: ExpenseService;
  let httpMock: HttpTestingController;

  const apiUrl = 'https://localhost:7058/expenses';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ExpenseService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(ExpenseService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should get paged expenses with filters and sorting', () => {
    const response: PagedResponse<Expense> = {
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    };

    service
      .getExpenses(1, 10, 'Meals', 'lunch', 'amount', 'asc')
      .subscribe((result) => {
        expect(result).toEqual(response);
      });

    const req = httpMock.expectOne(
      `${apiUrl}?page=1&pageSize=10&sortBy=amount&sortDirection=asc&category=Meals&search=lunch`,
    );

    expect(req.request.method).toBe('GET');
    req.flush(response);
  });

  it('should get one expense by id', () => {
    const expense: Expense = {
      id: '123',
      description: 'Lunch',
      amount: 20,
      category: 'Meals',
      date: new Date().toISOString(),
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: new Date().toISOString(),
    };

    service.getExpense('123').subscribe((result) => {
      expect(result).toEqual(expense);
    });

    const req = httpMock.expectOne(`${apiUrl}/123`);
    expect(req.request.method).toBe('GET');
    req.flush(expense);
  });

  it('should create an expense', () => {
    const request: CreateExpenseRequest = {
      description: 'Lunch',
      amount: 20,
      category: 'Meals',
      date: new Date().toISOString(),
    };

    const response: Expense = {
      id: '123',
      ...request,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: new Date().toISOString(),
    };

    service.createExpense(request).subscribe((result) => {
      expect(result).toEqual(response);
    });

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(response);
  });

  it('should update an expense', () => {
    const request: CreateExpenseRequest = {
      description: 'Updated',
      amount: 42,
      category: 'Travel',
      date: new Date().toISOString(),
    };

    const response: Expense = {
      id: '123',
      ...request,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: new Date().toISOString(),
    };

    service.updateExpense('123', request).subscribe((result) => {
      expect(result).toEqual(response);
    });

    const req = httpMock.expectOne(`${apiUrl}/123`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(response);
  });

  it('should delete an expense', () => {
    service.deleteExpense('123').subscribe((result) => {
      expect(result).toBeNull();
    });

    const req = httpMock.expectOne(`${apiUrl}/123`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
