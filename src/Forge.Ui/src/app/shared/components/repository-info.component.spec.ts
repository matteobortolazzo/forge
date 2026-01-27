import { TestBed, ComponentFixture } from '@angular/core/testing';
import { RepositoryInfoComponent } from './repository-info.component';
import { RepositoryStore } from '../../core/stores/repository.store';
import { signal, Signal } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';
import { Repository } from '../models';

// Helper to create a readonly signal from a value
const createSignal = <T>(value: T): Signal<T> => signal(value).asReadonly();

describe('RepositoryInfoComponent', () => {
  let component: RepositoryInfoComponent;
  let fixture: ComponentFixture<RepositoryInfoComponent>;

  const mockRepository: Repository = {
    id: 'repo-1',
    name: 'test-repo',
    path: '/home/user/repos/test-repo',
    isActive: true,
    branch: 'main',
    commitHash: 'abc1234',
    remoteUrl: 'git@github.com:user/test-repo.git',
    isDirty: false,
    isGitRepository: true,
    lastRefreshedAt: new Date(),
    createdAt: new Date(),
    updatedAt: new Date(),
    taskCount: 5,
  };

  const createMockStore = (overrides: Partial<{
    loading: boolean;
    hasRepositories: boolean;
    info: Repository | null;
    name: string;
    path: string;
    displayBranch: string | null;
    isDirty: boolean;
    isGitRepository: boolean;
  }> = {}) => {
    const defaults = {
      loading: false,
      hasRepositories: true,
      info: mockRepository,
      name: 'test-repo',
      path: '/home/user/repos/test-repo',
      displayBranch: 'main',
      isDirty: false,
      isGitRepository: true,
    };
    const values = { ...defaults, ...overrides };

    return {
      loading: createSignal(values.loading),
      hasRepositories: createSignal(values.hasRepositories),
      info: createSignal(values.info),
      name: createSignal(values.name),
      path: createSignal(values.path),
      displayBranch: createSignal(values.displayBranch),
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

  describe('when no repositories exist', () => {
    beforeEach(async () => {
      TestBed.resetTestingModule();
      await setupComponent(createMockStore({
        hasRepositories: false,
        info: null,
        name: '',
        path: '',
        displayBranch: null,
      }));
    });

    it('should show no repository message', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('No repository selected');
    });
  });

  describe('when loading', () => {
    beforeEach(async () => {
      TestBed.resetTestingModule();
      await setupComponent(createMockStore({
        loading: true,
        hasRepositories: false,
        info: null,
        name: '',
        path: '',
      }));
    });

    it('should show loading indicator', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('Loading...');
    });
  });

  describe('when not a git repository', () => {
    beforeEach(async () => {
      TestBed.resetTestingModule();
      const nonGitRepo: Repository = {
        ...mockRepository,
        isGitRepository: false,
        branch: undefined,
        commitHash: undefined,
        remoteUrl: undefined,
      };
      await setupComponent(createMockStore({
        info: nonGitRepo,
        isGitRepository: false,
        displayBranch: null,
      }));
    });

    it('should not display branch section when not a git repo', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      // Branch separator "/" should not be present between name and branch
      const textContent = compiled.textContent || '';
      // Should have the repo name but not the branch separator with a branch
      expect(textContent).toContain('test-repo');
      // The separator "/" only appears when isGitRepository is true
      const separators = compiled.querySelectorAll('span');
      let hasBranchSeparator = false;
      separators.forEach(s => {
        if (s.textContent?.trim() === '/') {
          hasBranchSeparator = true;
        }
      });
      expect(hasBranchSeparator).toBe(false);
    });
  });
});
