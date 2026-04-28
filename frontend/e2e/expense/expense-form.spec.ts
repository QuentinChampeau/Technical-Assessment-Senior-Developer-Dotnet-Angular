import { Page } from '@playwright/test';

export const API_URL = 'https://localhost:7058';

export const EXPENSE_1 = {
  id: '550e8400-e29b-41d4-a716-446655440001',
  description: 'Team lunch at the office',
  amount: 42.5,
  category: 'Meals',
  date: '2026-04-01T12:00:00.000Z',
  createdAtUtc: '2026-04-01T12:00:00.000Z',
  updatedAtUtc: '2026-04-01T12:00:00.000Z',
};

export const EXPENSE_2 = {
  id: '550e8400-e29b-41d4-a716-446655440002',
  description: 'Train ticket to Paris',
  amount: 89.0,
  category: 'Travel',
  date: '2026-04-10T08:30:00.000Z',
  createdAtUtc: '2026-04-10T08:30:00.000Z',
  updatedAtUtc: '2026-04-10T08:30:00.000Z',
};

export const EXPENSE_3 = {
  id: '550e8400-e29b-41d4-a716-446655440003',
  description: 'Annual IDE subscription',
  amount: 249.99,
  category: 'Software',
  date: '2026-04-15T09:00:00.000Z',
  createdAtUtc: '2026-04-15T09:00:00.000Z',
  updatedAtUtc: '2026-04-15T09:00:00.000Z',
};

export const MOCK_EXPENSES = [EXPENSE_1, EXPENSE_2, EXPENSE_3];

export const MOCK_PAGED_RESPONSE = {
  items: MOCK_EXPENSES,
  page: 1,
  pageSize: 10,
  totalCount: 3,
  totalPages: 1,
};

export const MOCK_AUDIT_HISTORY = [
  {
    id: 1,
    entityName: 'Expense',
    entityId: EXPENSE_1.id,
    action: 'Created',
    changesJson: JSON.stringify({
      Id: EXPENSE_1.id,
      Description: EXPENSE_1.description,
      Amount: EXPENSE_1.amount,
      Category: EXPENSE_1.category,
    }),
    createdAtUtc: '2026-04-01T12:00:00.000Z',
  },
  {
    id: 2,
    entityName: 'Expense',
    entityId: EXPENSE_1.id,
    action: 'Updated',
    changesJson: JSON.stringify({
      OldValue: { Description: 'Team lunch', Amount: 35.0 },
      NewValue: {
        Description: EXPENSE_1.description,
        Amount: EXPENSE_1.amount,
      },
    }),
    createdAtUtc: '2026-04-02T10:00:00.000Z',
  },
];

/** Intercepts all GET /expenses calls (list + dashboard) with the mock paged response. */
export async function mockExpenseList(page: Page) {
  await page.route(`${API_URL}/expenses?*`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_PAGED_RESPONSE),
    });
  });
}

/** Intercepts GET /expenses/:id for the first mock expense. */
export async function mockExpenseById(page: Page) {
  await page.route(`${API_URL}/expenses/${EXPENSE_1.id}`, async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(EXPENSE_1),
      });
    } else {
      await route.continue();
    }
  });
}

/** Intercepts GET /expenses/:id/history for the first mock expense. */
export async function mockExpenseHistory(page: Page) {
  await page.route(
    `${API_URL}/expenses/${EXPENSE_1.id}/history`,
    async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_AUDIT_HISTORY),
      });
    },
  );
}
