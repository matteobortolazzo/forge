import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/queue/task-queue.component').then(m => m.TaskQueueComponent),
  },
  {
    path: 'board',
    loadComponent: () =>
      import('./features/board/board.component').then(m => m.BoardComponent),
  },
  {
    path: 'tasks/:id',
    loadComponent: () =>
      import('./features/task-detail/task-detail.component').then(
        m => m.TaskDetailComponent
      ),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
