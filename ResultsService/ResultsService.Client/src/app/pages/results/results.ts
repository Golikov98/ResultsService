import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';

import { ResultsApiService } from '../../services/results-api.service';
import { ResultDto } from '../../models/result.model';
import { ResultFilterDto } from '../../models/result-filter.model';

@Component({
  selector: 'app-results',
  imports: [DatePipe],
  templateUrl: './results.html',
  styleUrl: './results.css'
})
export class Results implements OnInit {

  private readonly resultsApi = inject(ResultsApiService);

  readonly results = signal<ResultDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly filter = signal<ResultFilterDto>({});

  ngOnInit(): void {
    this.loadResults();
  }

  loadResults(): void {
    this.loading.set(true);
    this.error.set('');

    this.resultsApi.getResults(this.filter()).subscribe({
      next: (results) => {
        console.log('Получены результаты:', results);

        this.results.set(results);
        this.loading.set(false);
      },

      error: (error) => {
        console.error('Ошибка загрузки результатов:', error);

        this.error.set('Не удалось загрузить результаты.');
        this.loading.set(false);
      }
    });
  }

  updateFilter(
    field: keyof ResultFilterDto,
    value: string
  ): void {
    if (value === '') {
      const updatedFilter = { ...this.filter() };
      delete updatedFilter[field];

      this.filter.set(updatedFilter);
      return;
    }

    this.filter.set({
      ...this.filter(),
      [field]: value
    });
  }

  updateNumberFilter(
    field: keyof ResultFilterDto,
    value: string
  ): void {
    if (value === '') {
      const updatedFilter = { ...this.filter() };
      delete updatedFilter[field];

      this.filter.set(updatedFilter);
      return;
    }

    this.filter.set({
      ...this.filter(),
      [field]: Number(value)
    });
  }

  resetFilter(): void {
    this.filter.set({});
    this.loadResults();
  }
}
