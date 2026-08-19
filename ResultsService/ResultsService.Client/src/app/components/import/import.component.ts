import { Component, inject, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { ResultsApiService } from '../../services/results-api.service';
import { ImportFileResponseDto } from '../../models/import-file-response.model';

@Component({
  selector: 'app-import',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './import.component.html',
  styleUrl: './import.component.css'
})
export class ImportComponent {

  private readonly resultsApi = inject(ResultsApiService);

  selectedFile: File | null = null;
  result: ImportFileResponseDto | null = null;

  errorMessage = '';
  isLoading = false;

  importCompleted = output<void>();

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;

    this.selectedFile = input.files?.[0] ?? null;
    this.result = null;
    this.errorMessage = '';
  }

  importFile(): void {
    if (!this.selectedFile) {
      this.errorMessage = 'Выберите CSV-файл.';
      return;
    }

    this.errorMessage = '';
    this.result = null;
    this.isLoading = true;

    this.resultsApi.importFile(this.selectedFile).subscribe({
      next: response => {
        this.result = response;
        this.isLoading = false;

        this.importCompleted.emit();
      },
      error: error => {
        this.isLoading = false;

        if (error.error?.message) {
          this.errorMessage = error.error.message;
        } else {
          this.errorMessage = 'Не удалось загрузить файл.';
        }
      }
    });
  }
}
