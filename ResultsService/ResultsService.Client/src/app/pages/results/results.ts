import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';

import { ResultsApiService } from '../../services/results-api.service';
import { ResultDto } from '../../models/result.model';
import { ResultFilterDto } from '../../models/result-filter.model';
import { ValueDto } from '../../models/value.model';

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

  readonly selectedFileName = signal<string | null>(null);
  readonly values = signal<ValueDto[]>([]);
  readonly valuesLoading = signal(false);
  readonly valuesError = signal('');

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

  showValues(fileName: string): void {
    this.selectedFileName.set(fileName);
    this.values.set([]);
    this.valuesError.set('');
    this.valuesLoading.set(true);

    this.resultsApi.getLatestValues(fileName).subscribe({
      next: (values) => {
        this.values.set(values);
        this.valuesLoading.set(false);
      },

      error: (error) => {
        console.error('Ошибка загрузки значений:', error);

        this.valuesError.set(
          'Не удалось загрузить значения для выбранного файла.'
        );

        this.valuesLoading.set(false);
      }
    });
  }
}
