import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/tasks/task-list/task-list.component').then(m => m.TaskListComponent),
  },
  {
    path: 'stream',
    loadComponent: () =>
      import('./features/tasks/task-stream/task-stream.component').then(m => m.TaskStreamComponent),
  },
  { path: '**', redirectTo: '' },
];
