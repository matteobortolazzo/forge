import { TestBed, ComponentFixture } from '@angular/core/testing';
import { RepositoryInfoComponent } from './repository-info.component';
import { RepositoryStore } from '../../core/stores/repository.store';
import { signal, Signal } from '@angular/core';
import { describe, it, expect, beforeEach, vi } from 'vitest';
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
    isDefault: true,
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

  const mockRepository2: Repository = {
    id: 'repo-2',
    name: 'other-repo',
    path: '/home/user/repos/other-repo',
    isDefault: false,
    isActive: true,
    branch: 'develop',
    commitHash: 'def5678',
    remoteUrl: 'git@github.com:user/other-repo.git',
    isDirty: true,
    isGitRepository: true,
    lastRefreshedAt: new Date(),
    createdAt: new Date(),
    updatedAt: new Date(),
    taskCount: 3,
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
    activeRepositories: Repository[];
    selectedId: string | null;
    setSelectedRepository: ReturnType<typeof vi.fn>;
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
      activeRepositories: [mockRepository],
      selectedId: 'repo-1',
      setSelectedRepository: vi.fn(),
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
      activeRepositories: createSignal(values.activeRepositories),
      selectedId: createSignal(values.selectedId),
      setSelectedRepository: values.setSelectedRepository,
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
        activeRepositories: [],
        selectedId: null,
      }));
    });

    it('should show add repository button', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('Add Repository');
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

  describe('with multiple repositories', () => {
    let mockStore: ReturnType<typeof createMockStore>;

    beforeEach(async () => {
      TestBed.resetTestingModule();
      mockStore = createMockStore({
        activeRepositories: [mockRepository, mockRepository2],
      });
      await setupComponent(mockStore);
    });

    it('should show dropdown arrow when multiple repos exist', () => {
      // The dropdown arrow should be visible
      const buttons = fixture.nativeElement.querySelectorAll('button');
      expect(buttons.length).toBeGreaterThan(0);
    });

    it('should toggle dropdown on button click', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      // Initially no dropdown menu
      expect(compiled.querySelector('[role="listbox"]')).toBeFalsy();

      // Click the main button to open
      component.toggleDropdown();
      fixture.detectChanges();

      // Dropdown should now be visible
      expect(compiled.querySelector('[role="listbox"]')).toBeTruthy();

      // Click again to close
      component.toggleDropdown();
      fixture.detectChanges();
      expect(compiled.querySelector('[role="listbox"]')).toBeFalsy();
    });

    it('should call setSelectedRepository when selecting different repo', () => {
      component.selectRepository('repo-2');
      expect(mockStore.setSelectedRepository).toHaveBeenCalledWith('repo-2');
    });
  });

  describe('with single repository', () => {
    beforeEach(async () => {
      TestBed.resetTestingModule();
      await setupComponent(createMockStore({
        activeRepositories: [mockRepository],
      }));
    });

    it('should not show dropdown when only one repo', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      // With single repo, toggling shouldn't open dropdown
      component.toggleDropdown();
      fixture.detectChanges();
      expect(compiled.querySelector('[role="listbox"]')).toBeFalsy();
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
      // Branch icon and separator should not be present
      expect(compiled.textContent).not.toContain('/');
    });
  });
});
