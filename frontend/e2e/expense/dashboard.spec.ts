import { test, expect } from '@playwright/test';
import {
  API_URL,
  EXPENSE_1,
  MOCK_EXPENSES,
  mockExpenseById,
  mockExpenseHistory,
  mockExpenseList,
} from './fixtures';

test.describe('Expense Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await mockExpenseList(page);
    await page.goto('/expenses');
  });

  test('shows the dashboard heading and subtitle', async ({ page }) => {
    await expect(
      page.getByRole('heading', { name: 'Expense Dashboard' }),
    ).toBeVisible();
    await expect(
      page.getByText('Overview of your tracked expenses'),
    ).toBeVisible();
  });

  test('renders all four summary stat cards', async ({ page }) => {
    await expect(page.getByText('Total Expenses')).toBeVisible();
    await expect(page.getByText('Total Amount')).toBeVisible();
    await expect(page.getByText('Average Amount')).toBeVisible();
    await expect(page.getByText('Top Category')).toBeVisible();
  });

  test('stat cards show computed values from the mock data', async ({
    page,
  }) => {
    // 3 mock expenses: 42.50 + 89.00 + 249.99 = 381.49 total, avg = 127.16
    // Scope each assertion to its specific card to avoid strict-mode violations
    // caused by '3', '381.49', etc. appearing in multiple elements (pagination,
    // category breakdown sidebar, etc.)
    const card = (label: string) =>
      page
        .locator('.bg-white.shadow.rounded-lg.p-5')
        .filter({ hasText: label });

    await expect(card('Total Expenses').locator('.text-2xl')).toHaveText('3');
    await expect(card('Total Amount').locator('.text-2xl')).toContainText(
      '381.49',
    );
    await expect(card('Average Amount').locator('.text-2xl')).toContainText(
      '127.16',
    );
    // Software has the highest amount (249.99) → top category
    await expect(card('Top Category').locator('.text-2xl')).toHaveText(
      'Software',
    );
  });

  test('expense table shows all mock expenses', async ({ page }) => {
    // Scope category checks to tbody: categories also appear in the filter
    // dropdown options and the category breakdown sidebar, which would cause
    // strict-mode violations with a bare getByText().
    const tbody = page.locator('tbody');
    for (const expense of MOCK_EXPENSES) {
      await expect(page.getByText(expense.description)).toBeVisible();
      await expect(
        tbody.getByText(expense.category, { exact: true }),
      ).toBeVisible();
    }
  });

  test('"Add New Expense" button navigates to the create form', async ({
    page,
  }) => {
    await page.getByRole('link', { name: 'Add New Expense' }).click();
    await expect(page).toHaveURL('/expenses/new');
    await expect(
      page.getByRole('heading', { name: 'Create New Expense' }),
    ).toBeVisible();
  });

  test('clicking an expense row navigates to the detail page', async ({
    page,
  }) => {
    await mockExpenseById(page);
    await mockExpenseHistory(page);

    await page.getByText(EXPENSE_1.description).click();
    await expect(page).toHaveURL(`/expenses/${EXPENSE_1.id}`);
  });

  test('shows category breakdown sidebar', async ({ page }) => {
    await expect(page.getByText('Category Breakdown')).toBeVisible();
    await expect(page.getByText('Monthly Totals')).toBeVisible();
  });

  test('shows an error banner when the API fails', async ({ page }) => {
    await page.unroute(`${API_URL}/expenses?*`);
    await page.route(`${API_URL}/expenses?*`, (route) =>
      route.fulfill({ status: 500 }),
    );
    await page.goto('/expenses');
    await expect(page.getByText(/failed to load/i).first()).toBeVisible();
  });
});
