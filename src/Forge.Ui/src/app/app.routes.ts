import { Routes } from '@angular/router';

export const routes: Routes = [
  // Default route - redirect to backlog
  {
    path: '',
    redirectTo: 'backlog',
    pathMatch: 'full',
  },
  // Backlog list (kanban board)
  {
    path: 'backlog',
    loadComponent: () =>
      import('./features/backlog/backlog-list.component').then(m => m.BacklogListComponent),
  },
  // Backlog item detail
  {
    path: 'backlog/:id',
    loadComponent: () =>
      import('./features/backlog/backlog-item-detail.component').then(
        m => m.BacklogItemDetailComponent
      ),
  },
  // Task list for a backlog item
  {
    path: 'backlog/:backlogItemId/tasks',
    loadComponent: () =>
      import('./features/queue/task-queue.component').then(m => m.TaskQueueComponent),
  },
  // Task detail
  {
    path: 'backlog/:backlogItemId/tasks/:id',
    loadComponent: () =>
      import('./features/task-detail/task-detail.component').then(
        m => m.TaskDetailComponent
      ),
  },
  // Catch-all redirect
  {
    path: '**',
    redirectTo: 'backlog',
  },
];
