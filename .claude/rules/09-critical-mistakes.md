# Critical Mistakes to Avoid

## Frontend Testing

**WRONG: Using Jasmine patterns**
```typescript
// DO NOT USE
jasmine.createSpy('myFn');
spyOn(service, 'method');
jasmine.createSpyObj('mock', ['method']);
```

**CORRECT: Using Vitest patterns**
```typescript
// USE THIS
import { vi } from 'vitest';
vi.fn();
vi.spyOn(service, 'method');
{ method: vi.fn() };
```

## Angular Control Flow

**WRONG: Using structural directives**
```html
<div *ngIf="condition">...</div>
<div *ngFor="let item of items">...</div>
<div [ngSwitch]="value">...</div>
```

**CORRECT: Using built-in control flow**
```html
@if (condition) { <div>...</div> }
@for (item of items; track item.id) { <div>...</div> }
@switch (value) { @case ('a') { ... } }
```

## Backend Service Lifetimes

**WRONG: Injecting scoped service into singleton**
```csharp
// DO NOT inject ForgeDbContext (scoped) into AgentRunnerService (singleton)
public class AgentRunnerService
{
    private readonly ForgeDbContext _db; // WRONG - stale context
}
```

**CORRECT: Using IServiceScopeFactory**
```csharp
public class AgentRunnerService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public async Task DoWork()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();
    }
}
```

## SSE Event Emission

**WRONG: Forgetting to emit SSE events**
```csharp
// Missing SSE emission after state change
task.State = newState;
await _db.SaveChangesAsync();
// Forgot: await _sseService.SendEventAsync(...)
```

**CORRECT: Always emit events**
```csharp
task.State = newState;
await _db.SaveChangesAsync();
await _sseService.SendEventAsync("task:updated", TaskDto.FromEntity(task));
```

## Human Gate Handling

**WRONG: Transitioning backlog item or task without checking pending gate**
```csharp
// DO NOT skip gate check
backlogItem.State = BacklogItemState.Ready;
task.State = PipelineState.Planning;
```

**CORRECT: Check HasPendingGate before transition**
```csharp
// For backlog items
if (backlogItem.HasPendingGate)
{
    throw new InvalidOperationException("Backlog item has pending human gate");
}
backlogItem.State = BacklogItemState.Ready;

// For tasks
if (task.HasPendingGate)
{
    throw new InvalidOperationException("Task has pending human gate");
}
task.State = PipelineState.Planning;
```

## JSON Serialization in Tests

**WRONG: Using default serialization options**
```csharp
var response = await client.PostAsJsonAsync("/api/tasks", dto);
// May fail due to enum serialization
```

**CORRECT: Using shared JsonOptions**
```csharp
var response = await client.PostAsJsonAsync("/api/tasks", dto, HttpClientExtensions.JsonOptions);
```
