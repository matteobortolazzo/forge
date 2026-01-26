import { TestBed, ComponentFixture } from '@angular/core/testing';
import { RepositoryInfoComponent } from './repository-info.component';
import { RepositoryStore } from '../../core/stores/repository.store';
import { signal, Signal } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';

// Helper to create a readonly signal from a value
const createSignal = <T>(value: T): Signal<T> => signal(value).asReadonly();

describe('RepositoryInfoComponent', () => {
  let component: RepositoryInfoComponent;
  let fixture: ComponentFixture<RepositoryInfoComponent>;

  const createMockStore = (overrides: Partial<{
    loading: boolean;
    error: string | null;
    name: string;
    path: string;
    branch: string | null;
    displayBranch: string | null;
    commitHash: string | null;
    remoteUrl: string | null;
    isDirty: boolean;
    isGitRepository: boolean;
  }> = {}) => {
    const defaults = {
      loading: false,
      error: null,
      name: 'test-repo',
      path: '/home/user/repos/test-repo',
      branch: 'main',
      displayBranch: 'main',
      commitHash: 'abc1234',
      remoteUrl: 'git@github.com:user/test-repo.git',
      isDirty: false,
      isGitRepository: true,
    };
    const values = { ...defaults, ...overrides };

    return {
      loading: createSignal(values.loading),
      error: createSignal(values.error),
      name: createSignal(values.name),
      path: createSignal(values.path),
      branch: createSignal(values.branch),
      displayBranch: createSignal(values.displayBranch),
      commitHash: createSignal(values.commitHash),
      remoteUrl: createSignal(values.remoteUrl),
      isDirty: createSignal(values.isDirty),
      isGitRepository: createSignal(values.isGitRepository),
    } as unknown as RepositoryStore;
  };

  const setupComponent = async (mockStore: ReturnType<typeof createMockStore>) => {
    await TestBed.configureTestingModule({
      imports: [RepositoryInfoComponent],
      providers: [{ provide: RepositoryStore, useValue: mockStore }],
    }).compileComponents();

    fixture = TestBed.createComponent(RepositoryInfoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  beforeEach(async () => {
    TestBed.resetTestingModule();
    await setupComponent(createMockStore());
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display repository name', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('test-repo');
  });

  it('should display branch when git repository', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('main');
  });

  it('should display commit hash when git repository', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('abc1234');
  });

  it('should have title attribute with full path on name element', () => {
    const nameElement = fixture.nativeElement.querySelector('[title="/home/user/repos/test-repo"]');
    expect(nameElement).toBeTruthy();
  });

  describe('when repository is dirty', () => {
    beforeEach(async () => {
      TestBed.resetTestingModule();
      await setupComponent(createMockStore({ isDirty: true }));
    });

    it('should show dirty indicator when isDirty is true', () => {
      const dirtyIndicator = fixture.nativeElement.querySelector('[title="Uncommitted changes"]');
      expect(dirtyIndicator).toBeTruthy();
    });
  });

  describe('when not a git repository', () => {
    beforeEach(async () => {
      TestBed.resetTestingModule();
      await setupComponent(createMockStore({
        name: 'test-folder',
        path: '/home/user/test-folder',
        branch: null,
        displayBranch: null,
        commitHash: null,
        remoteUrl: null,
        isGitRepository: false,
      }));
    });

    it('should show "not a git repo" when isGitRepository is false', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('not a git repo');
    });

    it('should not display branch when not a git repo', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).not.toContain('main');
    });
  });

  describe('when loading', () => {
    beforeEach(async () => {
      TestBed.resetTestingModule();
      await setupComponent(createMockStore({
        loading: true,
        name: '',
        path: '',
      }));
    });

    it('should show loading indicator', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('Loading...');
    });
  });
});
