
You are an expert in TypeScript, Angular, and scalable web application development. You write functional, maintainable, performant, and accessible code following Angular and TypeScript best practices.

## TypeScript Best Practices

- Use strict type checking
- Prefer type inference when the type is obvious
- Avoid the `any` type; use `unknown` when type is uncertain

## Angular Best Practices

- Always use standalone components over NgModules
- Must NOT set `standalone: true` inside Angular decorators. It's the default in Angular v20+.
- Use signals for state management
- Implement lazy loading for feature routes
- Do NOT use the `@HostBinding` and `@HostListener` decorators. Put host bindings inside the `host` object of the `@Component` or `@Directive` decorator instead
- Use `NgOptimizedImage` for all static images.
  - `NgOptimizedImage` does not work for inline base64 images.

## Accessibility Requirements

- It MUST pass all AXE checks.
- It MUST follow all WCAG AA minimums, including focus management, color contrast, and ARIA attributes.

### Components

- Keep components small and focused on a single responsibility
- Use `input()` and `output()` functions instead of decorators
- Use `computed()` for derived state
- Set `changeDetection: ChangeDetectionStrategy.OnPush` in `@Component` decorator
- Prefer inline templates for small components
- Prefer Reactive forms instead of Template-driven ones
- Do NOT use `ngClass`, use `class` bindings instead
- Do NOT use `ngStyle`, use `style` bindings instead
- When using external templates/styles, use paths relative to the component TS file.

## State Management

- Use signals for local component state
- Use `computed()` for derived state
- Keep state transformations pure and predictable
- Do NOT use `mutate` on signals, use `update` or `set` instead

## Templates

- Keep templates simple and avoid complex logic
- Use native control flow (`@if`, `@for`, `@switch`) instead of `*ngIf`, `*ngFor`, `*ngSwitch`
- Use the async pipe to handle observables
- Do not assume globals like (`new Date()`) are available.
- Do not write arrow functions in templates (they are not supported).

## Services

- Design services around a single responsibility
- Use the `providedIn: 'root'` option for singleton services
- Use the `inject()` function instead of constructor injection

## Testing

This project uses **Vitest 4.x** for unit testing. Do NOT use Jasmine or Jest patterns.

### Required Patterns
- Use `vi.fn()` for creating mock functions
- Use `vi.spyOn(obj, 'method')` for spying on methods
- Use `vi.mock('module')` for module mocking
- Import test utilities from `vitest` when explicit imports are needed

### Forbidden Patterns
- Do NOT use `jasmine.createSpy()` or `jasmine.createSpyObj()`
- Do NOT use `spyOn()` without the `vi.` prefix
- Do NOT use Jest-style `jest.fn()` or `jest.mock()`

### Example Component Test
```typescript
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { signal } from '@angular/core';
import { of } from 'rxjs';
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

  it('should call service method', () => {
    component.loadData();
    expect(serviceMock.getData).toHaveBeenCalled();
  });
});
```

### Example Store/Service Test
```typescript
import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';
import { MyStore } from './my.store';
import { MyService } from '../services/my.service';

describe('MyStore', () => {
  let store: MyStore;
  let serviceMock: { fetchData: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    serviceMock = {
      fetchData: vi.fn().mockReturnValue(of([{ id: '1', name: 'Test' }])),
    };

    TestBed.configureTestingModule({
      providers: [
        MyStore,
        { provide: MyService, useValue: serviceMock },
      ],
    });

    store = TestBed.inject(MyStore);
  });

  it('should load data', () => {
    store.loadData();
    expect(store.items()).toHaveLength(1);
  });
});
```
