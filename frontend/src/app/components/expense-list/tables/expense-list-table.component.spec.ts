import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ExpenseListTableComponent } from './expense-list-table.component';
import { provideRouter } from '@angular/router';
import { Expense } from '../../../models/expense.model';

describe('ExpenseListTableComponent', () => {
  let component: ExpenseListTableComponent;
  let fixture: ComponentFixture<ExpenseListTableComponent>;

  const expenses: Expense[] = [
    {
      id: '1',
      description: 'Lunch',
      amount: 20,
      category: 'Meals',
      date: '2026-04-23T00:00:00Z',
      createdAtUtc: '2026-04-23T10:00:00Z',
      updatedAtUtc: '2026-04-23T10:00:00Z',
    },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExpenseListTableComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(ExpenseListTableComponent);
    component = fixture.componentInstance;

    fixture.componentRef.setInput('expenses', expenses);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('page', 1);
    fixture.componentRef.setInput('pageSize', 10);
    fixture.componentRef.setInput('totalCount', 1);
    fixture.componentRef.setInput('sortBy', 'date');
    fixture.componentRef.setInput('sortDirection', 'desc');

    fixture.detectChanges();
  });

  it('should display expenses', () => {
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Lunch');
    expect(compiled.textContent).toContain('Meals');
    expect(compiled.textContent).toContain('20.00');
  });

  it('should emit sortChange when sorting by amount', () => {
    spyOn(component.sortChange, 'emit');

    component.onSort('amount', new Event('click'));

    expect(component.sortChange.emit).toHaveBeenCalledWith('amount');
  });

  it('should emit delete when deleting an expense', () => {
    spyOn(component.delete, 'emit');

    component.onDelete('1', new Event('click'));

    expect(component.delete.emit).toHaveBeenCalledWith('1');
  });

  it('should emit filtersChange when applying filters', () => {
    spyOn(component.filtersChange, 'emit');

    component.search = 'lunch';
    component.selectedCategory = 'Meals';

    component.applyFilters();

    expect(component.filtersChange.emit).toHaveBeenCalledWith({
      search: 'lunch',
      category: 'Meals',
    });
  });

  it('should clear filters and emit empty filter values', () => {
    spyOn(component.filtersChange, 'emit');

    component.search = 'lunch';
    component.selectedCategory = 'Meals';

    component.clearFilters();

    expect(component.search).toBe('');
    expect(component.selectedCategory).toBe('');
    expect(component.filtersChange.emit).toHaveBeenCalledWith({
      search: '',
      category: '',
    });
  });
});
