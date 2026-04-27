import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ExpenseFormComponent } from './expense-form.component';
import { ExpenseService } from '../../services/expense.service';
import { provideRouter, Router } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';

describe('ExpenseFormComponent', () => {
  let component: ExpenseFormComponent;
  let fixture: ComponentFixture<ExpenseFormComponent>;
  let expenseService: jasmine.SpyObj<ExpenseService>;
  let router: Router;

  beforeEach(async () => {
    expenseService = jasmine.createSpyObj<ExpenseService>('ExpenseService', [
      'createExpense',
      'updateExpense',
      'getExpense',
    ]);

    await TestBed.configureTestingModule({
      imports: [ExpenseFormComponent],
      providers: [
        provideRouter([]),
        {
          provide: ExpenseService,
          useValue: expenseService,
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: () => null,
              },
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ExpenseFormComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);

    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));

    fixture.detectChanges();
  });

  it('should create the form with default values', () => {
    expect(component.expenseForm).toBeTruthy();
    expect(component.expenseForm.get('amount')).toBeTruthy();
    expect(component.expenseForm.get('category')).toBeTruthy();
    expect(component.expenseForm.get('date')).toBeTruthy();
    expect(component.expenseForm.get('description')).toBeTruthy();
  });

  it('should mark form as invalid when required fields are missing', () => {
    component.expenseForm.patchValue({
      amount: null,
      category: '',
      description: '',
    });

    expect(component.expenseForm.invalid).toBeTrue();
  });

  it('should create expense when form is valid', () => {
    const createdExpense = {
      id: '123',
      description: 'Lunch',
      amount: 20,
      category: 'Meals',
      date: new Date().toISOString(),
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: new Date().toISOString(),
    };

    expenseService.createExpense.and.returnValue(of(createdExpense));

    component.expenseForm.patchValue({
      amount: 20,
      category: 'Meals',
      date: '2026-04-23',
      description: 'Lunch expense',
    });

    component.onSubmit();

    expect(expenseService.createExpense).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/expenses', '123']);
  });

  it('should display an error when create fails', () => {
    expenseService.createExpense.and.returnValue(
      throwError(() => ({ status: 500 })),
    );

    component.expenseForm.patchValue({
      amount: 20,
      category: 'Meals',
      date: '2026-04-23',
      description: 'Lunch expense',
    });

    component.onSubmit();

    expect(component.error).toBe('Failed to create expense');
    expect(component.loading).toBeFalse();
  });
});
