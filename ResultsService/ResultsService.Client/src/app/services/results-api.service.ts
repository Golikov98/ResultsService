import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';

import { ImportFileResponseDto } from '../models/import-file-response.model';
import { ResultDto } from '../models/result.model';
import { ResultFilterDto } from '../models/result-filter.model';
import { ValueDto } from '../models/value.model';

@Injectable({
  providedIn: 'root'
})
export class ResultsApiService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiUrl}/Results`;

  importFile(file: File): Observable<ImportFileResponseDto> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<ImportFileResponseDto>(
      `${this.apiUrl}/import`,
      formData
    );
  }

  getResults(filter?: ResultFilterDto): Observable<ResultDto[]> {
    let params = new HttpParams();

    if (filter) {
      if (filter.fileName) {
        params = params.set('FileName', filter.fileName);
      }

      if (filter.startDateFrom) {
        params = params.set('StartDateFrom', filter.startDateFrom);
      }

      if (filter.startDateTo) {
        params = params.set('StartDateTo', filter.startDateTo);
      }

      if (filter.averageValueFrom !== undefined) {
        params = params.set(
          'AverageValueFrom',
          filter.averageValueFrom.toString()
        );
      }

      if (filter.averageValueTo !== undefined) {
        params = params.set(
          'AverageValueTo',
          filter.averageValueTo.toString()
        );
      }

      if (filter.averageExecutionTimeFrom !== undefined) {
        params = params.set(
          'AverageExecutionTimeFrom',
          filter.averageExecutionTimeFrom.toString()
        );
      }

      if (filter.averageExecutionTimeTo !== undefined) {
        params = params.set(
          'AverageExecutionTimeTo',
          filter.averageExecutionTimeTo.toString()
        );
      }
    }

    return this.http.get<ResultDto[]>(
      this.apiUrl,
      { params }
    );
  }

  getLatestValues(fileName: string): Observable<ValueDto[]> {
    return this.http.get<ValueDto[]>(
      `${this.apiUrl}/${encodeURIComponent(fileName)}/values`
    );
  }
}

