import { test, expect } from '@playwright/test';
import {
  API_URL,
  EXPENSE_1,
  MOCK_AUDIT_HISTORY,
  mockExpenseById,
  mockExpenseHistory,
  mockExpenseList,
} from './fixtures';

test.describe('Expense Detail', () => {
  test.beforeEach(async ({ page }) => {
    await mockExpenseById(page);
    await mockExpenseHistory(page);
    await page.goto(`/expenses/${EXPENSE_1.id}`);
  });

  test('shows the "Expense Details" heading', async ({ page }) => {
    await expect(
      page.getByRole('heading', { name: 'Expense Details' }),
    ).toBeVisible();
  });

  test('displays all expense fields', async ({ page }) => {
    await expect(page.getByText(EXPENSE_1.description)).toBeVisible();
    await expect(page.getByText(EXPENSE_1.category)).toBeVisible();
    // amount formatted as 42.50
    await expect(page.getByText('42.50')).toBeVisible();
    // expense ID is shown in the detail card
    await expect(page.getByText(EXPENSE_1.id)).toBeVisible();
  });

  test('has a back link that returns to the dashboard', async ({ page }) => {
    await mockExpenseList(page);
    await page.getByRole('link', { name: 'Back to Expenses' }).click();
    await expect(page).toHaveURL('/expenses');
  });

  test('shows the change history table with correct entries', async ({
    page,
  }) => {
    await expect(
      page.getByRole('heading', { name: 'Change History' }),
    ).toBeVisible();

    for (const entry of MOCK_AUDIT_HISTORY) {
      // exact: true prevents matching "Created At" / "Updated At" field labels
      await expect(page.getByText(entry.action, { exact: true })).toBeVisible();
    }
  });

  test('audit entry badges have the correct action labels', async ({
    page,
  }) => {
    // exact: true distinguishes the badge text from the "Created At" / "Updated At" field labels
    await expect(page.getByText('Created', { exact: true })).toBeVisible();
    await expect(page.getByText('Updated', { exact: true })).toBeVisible();
  });

  test('clicking "View" on an audit entry expands the JSON details', async ({
    page,
  }) => {
    const viewButtons = page.getByRole('button', { name: 'View' });
    await viewButtons.first().click();

    // Pre element with the JSON content should now be visible
    await expect(page.locator('pre').first()).toBeVisible();
    // The toggle button should now say "Hide"
    await expect(
      page.getByRole('button', { name: 'Hide' }).first(),
    ).toBeVisible();
  });

  test('clicking "Hide" after expanding collapses the details again', async ({
    page,
  }) => {
    await page.getByRole('button', { name: 'View' }).first().click();
    await page.getByRole('button', { name: 'Hide' }).first().click();

    await expect(page.locator('pre').first()).not.toBeVisible();
  });

  test('delete button shows a confirmation dialog', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.dismiss());
    await page.getByRole('button', { name: 'Delete' }).click();
    // After dismissing the dialog, we should still be on the detail page
    await expect(page).toHaveURL(`/expenses/${EXPENSE_1.id}`);
  });

  test('confirming delete navigates back to the expense list', async ({
    page,
  }) => {
    // The beforeEach handler only handles GET.  Registering a second handler
    // for the same URL works LIFO — it runs first and would call
    // route.continue() for GET, which goes to the real network and fails
    // (no server running), causing the component to hide the Delete button.
    // Solution: unroute the beforeEach handler and register one that covers
    // both GET and DELETE.
    await page.unroute(`${API_URL}/expenses/${EXPENSE_1.id}`);
    await page.route(`${API_URL}/expenses/${EXPENSE_1.id}`, async (route) => {
      if (route.request().method() === 'DELETE') {
        await route.fulfill({ status: 204 });
      } else {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(EXPENSE_1),
        });
      }
    });

    await mockExpenseList(page);

    // Re-navigate so Angular picks up the refreshed route handlers
    await page.goto(`/expenses/${EXPENSE_1.id}`);
    await expect(
      page.getByRole('heading', { name: 'Expense Details' }),
    ).toBeVisible();

    page.once('dialog', (dialog) => dialog.accept());
    await page.getByRole('button', { name: 'Delete' }).click();

    await expect(page).toHaveURL('/expenses');
  });

  test('shows an error message when the expense is not found', async ({
    page,
  }) => {
    await page.unroute(`${API_URL}/expenses/${EXPENSE_1.id}`);
    await page.route(`${API_URL}/expenses/${EXPENSE_1.id}`, async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 404 });
      } else {
        await route.continue();
      }
    });

    await page.goto(`/expenses/${EXPENSE_1.id}`);
    await expect(page.getByText('Expense not found')).toBeVisible();
  });
});
