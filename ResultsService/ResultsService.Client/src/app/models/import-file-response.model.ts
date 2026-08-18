import { ResultDto } from './result.model';

export interface ImportFileResponseDto {
  fileName: string;
  rowsCount: number;
  result: ResultDto;
}
