# Frontend Patterns

For detailed Forge.Ui implementation documentation, see `src/Forge.Ui/README.md` and `src/Forge.Ui/CLAUDE.md`.

## Component Organization

Standalone components with lazy loading:

```typescript
// app.routes.ts
export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/board/board.component').then(m => m.BoardComponent) },
  { path: 'tasks/:id', loadComponent: () => import('./features/task-detail/task-detail.component').then(m => m.TaskDetailComponent) },
];
```

## State Management with Signals

Use Angular Signals for reactive state:

```typescript
// core/stores/task.store.ts
@Injectable({ providedIn: 'root' })
export class TaskStore {
  private tasks = signal<Task[]>([]);

  readonly tasksByState = computed(() => {
    return this.tasks().reduce((acc, task) => {
      const state = task.state;
      if (!acc[state]) acc[state] = [];
      acc[state].push(task);
      return acc;
    }, {} as Record<string, Task[]>);
  });

  updateTask(updated: Task) {
    this.tasks.update(tasks =>
      tasks.map(t => t.id === updated.id ? updated : t)
    );
  }
}
```

## SSE Service

```typescript
// core/services/sse.service.ts
@Injectable({ providedIn: 'root' })
export class SseService {
  private eventSource: EventSource | null = null;

  connect(): Observable<ServerEvent> {
    return new Observable(observer => {
      this.eventSource = new EventSource('/api/events');

      this.eventSource.onmessage = (event) => {
        observer.next(JSON.parse(event.data));
      };

      return () => this.eventSource?.close();
    });
  }
}
```

## Angular 21 Conventions

- **Standalone Components**: All components standalone (no NgModules)
- **Signals**: Use signals for reactive state
- **Control Flow**: Use @if, @for, @switch (not *ngIf, *ngFor)
- **Zoneless**: Application runs in zoneless mode

## Quick Reference

- **7 feature components**: BoardComponent, TaskColumnComponent, TaskCardComponent, CreateTaskDialogComponent, TaskDetailComponent, AgentOutputComponent, NotificationPanelComponent
- **9 shared components**: StateBadge, PriorityBadge, AgentIndicator, ErrorAlert, LoadingSpinner, PausedBadge, SchedulerStatus, ArtifactTypeBadge, ArtifactPanel
- **6 signal stores**: TaskStore, AgentStore, LogStore, NotificationStore, SchedulerStore, ArtifactStore
- **5 services**: TaskService, AgentService, SseService, SchedulerService, ArtifactService (all with mock mode)
