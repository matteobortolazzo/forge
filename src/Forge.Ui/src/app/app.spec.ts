import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { signal, computed } from '@angular/core';
import { App } from './app';
import { RepositoryStore } from './core/stores/repository.store';
import { SseEventDispatcher } from './core/services/sse-event-dispatcher.service';

describe('App', () => {
  let repositoryStoreMock: {
    loadRepositories: ReturnType<typeof vi.fn>;
    hasRepositories: ReturnType<typeof signal<boolean>>;
    repositoriesWithInitials: ReturnType<typeof signal<unknown[]>>;
    selectedId: ReturnType<typeof signal<string | null>>;
    setSelectedRepository: ReturnType<typeof vi.fn>;
  };
  let sseEventDispatcherMock: {
    connect: ReturnType<typeof vi.fn>;
    disconnect: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    repositoryStoreMock = {
      loadRepositories: vi.fn().mockResolvedValue(undefined),
      hasRepositories: signal(true),
      repositoriesWithInitials: signal([]),
      selectedId: signal(null),
      setSelectedRepository: vi.fn(),
    };

    sseEventDispatcherMock = {
      connect: vi.fn(),
      disconnect: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        { provide: RepositoryStore, useValue: repositoryStoreMock },
        { provide: SseEventDispatcher, useValue: sseEventDispatcherMock },
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should contain router outlet', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });

  describe('first-run experience', () => {
    it('should open add dialog when no repositories exist', async () => {
      repositoryStoreMock.hasRepositories = signal(false);

      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;

      await app.ngOnInit();

      expect(app.isAddDialogOpen()).toBe(true);
    });

    it('should not open add dialog when repositories exist', async () => {
      repositoryStoreMock.hasRepositories = signal(true);

      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;

      await app.ngOnInit();

      expect(app.isAddDialogOpen()).toBe(false);
    });

    it('should connect to SSE after loading repositories', async () => {
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;

      await app.ngOnInit();

      expect(repositoryStoreMock.loadRepositories).toHaveBeenCalled();
      expect(sseEventDispatcherMock.connect).toHaveBeenCalled();
    });
  });
});
