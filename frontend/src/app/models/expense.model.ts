export interface Expense {
  id: string;
  description: string;
  amount: number;
  category: string;
  date: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CreateExpenseRequest {
  description: string;
  amount: number;
  category: string;
  date: string;
}
