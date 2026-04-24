import {
  AfterViewInit,
  Component,
  DestroyRef,
  ElementRef,
  ViewChild,
  effect,
  input,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import Chart from 'chart.js/auto';
import { Expense } from '../../../models/expense.model';

@Component({
  selector: 'app-expense-dashboard-charts',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './expense-dashboard-charts.component.html',
})
export class ExpenseDashboardChartsComponent implements AfterViewInit {
  expenses = input<Expense[]>([]);

  @ViewChild('categoryChartCanvas')
  private categoryChartCanvas?: ElementRef<HTMLCanvasElement>;

  @ViewChild('monthlyChartCanvas')
  private monthlyChartCanvas?: ElementRef<HTMLCanvasElement>;

  private categoryChart?: Chart;
  private monthlyChart?: Chart;
  private viewInitialized = false;

  constructor(private readonly destroyRef: DestroyRef) {
    effect(() => {
      this.expenses();

      if (this.viewInitialized) {
        this.renderCharts();
      }
    });

    this.destroyRef.onDestroy(() => {
      this.categoryChart?.destroy();
      this.monthlyChart?.destroy();
    });
  }

  ngAfterViewInit(): void {
    this.viewInitialized = true;
    this.renderCharts();
  }

  private renderCharts(): void {
    this.renderCategoryChart();
    this.renderMonthlyChart();
  }

  private renderCategoryChart(): void {
    if (!this.categoryChartCanvas) {
      return;
    }

    const categoryTotals = new Map<string, number>();

    for (const expense of this.expenses()) {
      categoryTotals.set(
        expense.category,
        (categoryTotals.get(expense.category) ?? 0) + expense.amount,
      );
    }

    this.categoryChart?.destroy();

    this.categoryChart = new Chart(this.categoryChartCanvas.nativeElement, {
      type: 'doughnut',
      data: {
        labels: Array.from(categoryTotals.keys()),
        datasets: [
          {
            data: Array.from(categoryTotals.values()),
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'bottom',
          },
        },
      },
    });
  }

  private renderMonthlyChart(): void {
    if (!this.monthlyChartCanvas) {
      return;
    }

    const monthlyTotals = new Map<string, number>();

    for (const expense of this.expenses()) {
      const date = new Date(expense.date);
      const month = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;

      monthlyTotals.set(
        month,
        (monthlyTotals.get(month) ?? 0) + expense.amount,
      );
    }

    const sortedMonthlyTotals = Array.from(monthlyTotals.entries()).sort(
      (a, b) => a[0].localeCompare(b[0]),
    );

    this.monthlyChart?.destroy();

    this.monthlyChart = new Chart(this.monthlyChartCanvas.nativeElement, {
      type: 'bar',
      data: {
        labels: sortedMonthlyTotals.map(([month]) => month),
        datasets: [
          {
            label: 'Monthly total',
            data: sortedMonthlyTotals.map(([, total]) => total),
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: false,
          },
        },
        scales: {
          y: {
            beginAtZero: true,
          },
        },
      },
    });
  }
}
