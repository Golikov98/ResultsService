import { Routes } from '@angular/router';
import { Results } from './pages/results/results';
import { ImportComponent } from './components/import/import.component';

export const routes: Routes = [
  {
    path: '',
    component: ImportComponent
  },
  {
    path: 'results',
    component: Results
  },
  {
    path: '**',
    redirectTo: ''
  }
];
