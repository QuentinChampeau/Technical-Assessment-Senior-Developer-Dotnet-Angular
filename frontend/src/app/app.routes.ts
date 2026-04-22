import { Routes } from '@angular/router';
import { ExpenseListComponent } from './components/expense-list/expense-list.component';
import { ExpenseDetailComponent } from './components/expense-detail/expense-detail.component';
import { ExpenseFormComponent } from './components/expense-form/expense-form.component';

export const routes: Routes = [
  { path: '', redirectTo: 'expenses', pathMatch: 'full' },
  { path: 'expenses', component: ExpenseListComponent },
  { path: 'expenses/new', component: ExpenseFormComponent },
  { path: 'expenses/:id', component: ExpenseDetailComponent },
  { path: '**', redirectTo: 'expenses' }
];
