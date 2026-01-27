# Testing Patterns

## Frontend Testing (Vitest)

The frontend uses **Vitest 4.x** (NOT Jasmine, NOT Jest) for unit testing.

**Key Patterns:**
- Test files: `*.spec.ts` co-located with source files
- Use `vi.fn()` for mocks, NOT `jasmine.createSpy()`
- Use `vi.spyOn()` for spying, NOT `spyOn()`
- Import from `vitest` when explicit imports are needed
- Combine with Angular TestBed for component testing

**Example Component Test:**
```typescript
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { MyComponent } from './my.component';
import { MyService } from '../../core/services/my.service';

describe('MyComponent', () => {
  let component: MyComponent;
  let fixture: ComponentFixture<MyComponent>;
  let serviceMock: { getData: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    serviceMock = {
      getData: vi.fn().mockReturnValue(of('result')),
    };

    await TestBed.configureTestingModule({
      imports: [MyComponent],
      providers: [{ provide: MyService, useValue: serviceMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(MyComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
```

**Vitest vs Jasmine Quick Reference:**

| DO NOT USE (Jasmine)       | USE THIS (Vitest)              |
|----------------------------|--------------------------------|
| `jasmine.createSpy()`      | `vi.fn()`                      |
| `spyOn(obj, 'method')`     | `vi.spyOn(obj, 'method')`      |
| `jasmine.createSpyObj()`   | Manual mock object with `vi.fn()` |
| No explicit imports needed | `import { vi } from 'vitest'` |

## Backend Integration Tests

Located in `src/Forge.Api/tests/Forge.Api.IntegrationTests/`. Uses `WebApplicationFactory` with SQLite in-memory database.

**Project Structure:**
```
Forge.Api.IntegrationTests/
├── Infrastructure/
│   ├── ForgeWebApplicationFactory.cs  # Test server with mocked services
│   └── ApiCollection.cs               # Shared fixture collection
├── Features/
│   └── Tasks/
│       ├── CreateTaskTests.cs         # Task creation tests
│       └── TransitionTaskTests.cs     # State transition tests
├── Helpers/
│   ├── HttpClientExtensions.cs        # JSON helpers with shared JsonOptions
│   ├── TestDatabaseHelper.cs          # Seed data utilities
│   └── Builders/                       # Test data builders
└── GlobalUsings.cs
```

## E2E Testing with Playwright

Located in `src/Forge.Ui/e2e/`. Uses Playwright for browser automation with a mock Claude CLI backend.

**Mock Infrastructure:**

The mock system replaces the real Claude Code CLI with a configurable mock client that simulates agent behavior:

- **MockClaudeAgentClient**: Simulates CLI responses with configurable delays and outputs
- **MockScenarioProvider**: Manages scenario selection based on task title patterns
- **MockEndpoints**: API endpoints for controlling mock behavior during tests

**Pre-built Scenarios:**

| Scenario | Behavior | Use Case |
|----------|----------|----------|
| `Default` | 3-second delay, success response | Standard testing |
| `QuickSuccess` | Instant success | Fast test execution |
| `Error` | Simulated failure | Error handling tests |
| `LongRunning` | 30-second delay | Timeout/abort tests |

**Environment Toggle:**

Set `CLAUDE_MOCK_MODE=true` to enable mock mode. The `e2e` launch profile configures this automatically.

**Mock Control API:**

During E2E tests, use the mock control endpoints to configure behavior:
```typescript
// Set scenario for specific task pattern
await fetch('/api/mock/scenario', {
  method: 'POST',
  body: JSON.stringify({ pattern: 'error-task', scenarioName: 'Error' })
});

// Reset to defaults
await fetch('/api/mock/reset', { method: 'POST' });
```
