import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'expenses', pathMatch: 'full' },
  {
    path: 'expenses',
    loadComponent: () =>
      import('./components/expense-list/expense-list.component').then(
        (m) => m.ExpenseListComponent,
      ),
  },
  {
    path: 'expenses/new',
    loadComponent: () =>
      import('./components/expense-form/expense-form.component').then(
        (m) => m.ExpenseFormComponent,
      ),
  },
  {
    path: 'expenses/:id',
    loadComponent: () =>
      import('./components/expense-detail/expense-detail.component').then(
        (m) => m.ExpenseDetailComponent,
      ),
  },
  {
    path: 'expenses/:id/edit',
    loadComponent: () =>
      import('./components/expense-form/expense-form.component').then(
        (m) => m.ExpenseFormComponent,
      ),
  },
  { path: '**', redirectTo: 'expenses' },
];
