import { TestBed } from '@angular/core/testing';
import { RepositoryStore } from './repository.store';
import { RepositoryService } from '../services/repository.service';
import { of, throwError } from 'rxjs';
import { Repository } from '../../shared/models';
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';

// Mock localStorage for Node test environment
const localStorageMock = (() => {
  let store: Record<string, string> = {};
  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => { store[key] = value; },
    removeItem: (key: string) => { delete store[key]; },
    clear: () => { store = {}; },
  };
})();

Object.defineProperty(globalThis, 'localStorage', { value: localStorageMock });

describe('RepositoryStore', () => {
  let store: RepositoryStore;
  let repositoryServiceMock: {
    getAll: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
    refresh: ReturnType<typeof vi.fn>;
    setDefault: ReturnType<typeof vi.fn>;
  };

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

  beforeEach(() => {
    // Clear localStorage before each test
    localStorageMock.clear();

    repositoryServiceMock = {
      getAll: vi.fn(),
      getById: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      refresh: vi.fn(),
      setDefault: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        RepositoryStore,
        { provide: RepositoryService, useValue: repositoryServiceMock },
      ],
    });

    store = TestBed.inject(RepositoryStore);
  });

  afterEach(() => {
    localStorageMock.clear();
  });

  it('should be created', () => {
    expect(store).toBeTruthy();
  });

  describe('loadRepositories', () => {
    it('should set repositories signal on success', async () => {
      repositoryServiceMock.getAll.mockReturnValue(of([mockRepository, mockRepository2]));

      await store.loadRepositories();

      expect(store.repositories()).toEqual([mockRepository, mockRepository2]);
    });

    it('should set loading to false after fetch completes', async () => {
      repositoryServiceMock.getAll.mockReturnValue(of([mockRepository]));

      await store.loadRepositories();

      expect(store.loading()).toBe(false);
    });

    it('should set error on failure', async () => {
      const errorMessage = 'Network error';
      repositoryServiceMock.getAll.mockReturnValue(throwError(() => new Error(errorMessage)));

      await store.loadRepositories();

      expect(store.error()).toBe(errorMessage);
      expect(store.repositories()).toEqual([]);
    });

    it('should auto-select default repository when no selection exists', async () => {
      repositoryServiceMock.getAll.mockReturnValue(of([mockRepository, mockRepository2]));

      await store.loadRepositories();

      expect(store.selectedId()).toBe('repo-1');
    });

    it('should auto-select first repository when no default exists', async () => {
      const nonDefaultRepo = { ...mockRepository, isDefault: false };
      repositoryServiceMock.getAll.mockReturnValue(of([nonDefaultRepo]));

      await store.loadRepositories();

      expect(store.selectedId()).toBe('repo-1');
    });

    it('should preserve selection from localStorage if valid', async () => {
      // First load sets up localStorage
      repositoryServiceMock.getAll.mockReturnValue(of([mockRepository, mockRepository2]));
      await store.loadRepositories();
      store.setSelectedRepository('repo-2');

      // Simulate what happens when repositories load - if selectedId is valid, keep it
      await store.loadRepositories();

      expect(store.selectedId()).toBe('repo-2');
    });
  });

  describe('setSelectedRepository', () => {
    beforeEach(async () => {
      repositoryServiceMock.getAll.mockReturnValue(of([mockRepository, mockRepository2]));
      await store.loadRepositories();
    });

    it('should update selected repository', () => {
      store.setSelectedRepository('repo-2');
      expect(store.selectedId()).toBe('repo-2');
    });

    it('should persist selection to localStorage', () => {
      store.setSelectedRepository('repo-2');
      expect(localStorage.getItem('forge:selectedRepositoryId')).toBe('repo-2');
    });
  });

  describe('computed signals', () => {
    beforeEach(async () => {
      repositoryServiceMock.getAll.mockReturnValue(of([mockRepository, mockRepository2]));
      await store.loadRepositories();
    });

    it('should return selected repository', () => {
      expect(store.selectedRepository()).toEqual(mockRepository);
    });

    it('should return default repository', () => {
      expect(store.defaultRepository()).toEqual(mockRepository);
    });

    it('should return active repositories', () => {
      expect(store.activeRepositories()).toEqual([mockRepository, mockRepository2]);
    });

    it('should return hasRepositories as true when repositories exist', () => {
      expect(store.hasRepositories()).toBe(true);
    });

    it('should return name from selected repository', () => {
      expect(store.name()).toBe('test-repo');
    });

    it('should return path from selected repository', () => {
      expect(store.path()).toBe('/home/user/repos/test-repo');
    });

    it('should return branch from selected repository', () => {
      expect(store.branch()).toBe('main');
    });

    it('should return isDirty from selected repository', () => {
      expect(store.isDirty()).toBe(false);
    });

    it('should return isGitRepository from selected repository', () => {
      expect(store.isGitRepository()).toBe(true);
    });

    it('should return info() as alias for selectedRepository', () => {
      expect(store.info()).toEqual(mockRepository);
    });
  });

  describe('displayBranch', () => {
    it('should return null when no branch', async () => {
      const repoWithoutBranch: Repository = {
        ...mockRepository,
        branch: undefined,
      };
      repositoryServiceMock.getAll.mockReturnValue(of([repoWithoutBranch]));
      await store.loadRepositories();

      expect(store.displayBranch()).toBeNull();
    });

    it('should return full branch name when under 30 chars', async () => {
      const shortBranchRepo: Repository = {
        ...mockRepository,
        branch: 'feature/short-name',
      };
      repositoryServiceMock.getAll.mockReturnValue(of([shortBranchRepo]));
      await store.loadRepositories();

      expect(store.displayBranch()).toBe('feature/short-name');
    });

    it('should truncate long branch names', async () => {
      const longBranchRepo: Repository = {
        ...mockRepository,
        branch: 'feature/this-is-a-very-long-branch-name-that-exceeds-thirty-characters',
      };
      repositoryServiceMock.getAll.mockReturnValue(of([longBranchRepo]));
      await store.loadRepositories();

      const displayBranch = store.displayBranch();
      expect(displayBranch).not.toBeNull();
      expect(displayBranch!.length).toBe(30);
      expect(displayBranch).toMatch(/\.\.\.$/);
    });
  });

  describe('default values before loading', () => {
    it('should return empty repositories array', () => {
      expect(store.repositories()).toEqual([]);
    });

    it('should return hasRepositories as false', () => {
      expect(store.hasRepositories()).toBe(false);
    });

    it('should return empty string for name', () => {
      expect(store.name()).toBe('');
    });

    it('should return empty string for path', () => {
      expect(store.path()).toBe('');
    });

    it('should return false for isDirty', () => {
      expect(store.isDirty()).toBe(false);
    });

    it('should return false for isGitRepository', () => {
      expect(store.isGitRepository()).toBe(false);
    });
  });

  describe('createRepository', () => {
    it('should add new repository to list', async () => {
      repositoryServiceMock.getAll.mockReturnValue(of([mockRepository]));
      await store.loadRepositories();

      const newRepo: Repository = { ...mockRepository2 };
      repositoryServiceMock.create.mockReturnValue(of(newRepo));

      const result = await store.createRepository({ name: 'other-repo', path: '/home/user/repos/other-repo' });

      expect(result).toEqual(newRepo);
      expect(store.repositories()).toHaveLength(2);
    });
  });

  describe('updateRepositoryFromEvent', () => {
    beforeEach(async () => {
      repositoryServiceMock.getAll.mockReturnValue(of([mockRepository]));
      await store.loadRepositories();
    });

    it('should update existing repository', () => {
      const updatedRepo: Repository = { ...mockRepository, name: 'updated-name' };
      store.updateRepositoryFromEvent(updatedRepo);

      expect(store.repositories()[0].name).toBe('updated-name');
    });

    it('should add new repository if not found', () => {
      store.updateRepositoryFromEvent(mockRepository2);

      expect(store.repositories()).toHaveLength(2);
    });
  });

  describe('removeRepositoryFromEvent', () => {
    beforeEach(async () => {
      repositoryServiceMock.getAll.mockReturnValue(of([mockRepository, mockRepository2]));
      await store.loadRepositories();
    });

    it('should remove repository by id', () => {
      store.removeRepositoryFromEvent('repo-2');

      expect(store.repositories()).toHaveLength(1);
      expect(store.repositories()[0].id).toBe('repo-1');
    });

    it('should select different repository if current is removed', () => {
      store.setSelectedRepository('repo-2');
      store.removeRepositoryFromEvent('repo-2');

      expect(store.selectedId()).toBe('repo-1');
    });
  });

  describe('getInitials', () => {
    it('should return single letter for single word name', () => {
      expect(store.getInitials('Forge')).toBe('F');
    });

    it('should return two letters for two word name', () => {
      expect(store.getInitials('My Project')).toBe('MP');
    });

    it('should return two letters for multiple word name', () => {
      expect(store.getInitials('Very Long Project Name')).toBe('VL');
    });

    it('should handle leading/trailing whitespace', () => {
      expect(store.getInitials('  Trimmed Name  ')).toBe('TN');
    });

    it('should handle uppercase input', () => {
      expect(store.getInitials('UPPERCASE NAME')).toBe('UN');
    });

    it('should handle lowercase input', () => {
      expect(store.getInitials('lowercase name')).toBe('LN');
    });
  });

  describe('repositoriesWithInitials', () => {
    beforeEach(async () => {
      repositoryServiceMock.getAll.mockReturnValue(of([mockRepository, mockRepository2]));
      await store.loadRepositories();
    });

    it('should include initials for each repository', () => {
      const repos = store.repositoriesWithInitials();
      expect(repos).toHaveLength(2);
      expect(repos[0].initials).toBe('T'); // 'test-repo' -> 'T'
      expect(repos[1].initials).toBe('O'); // 'other-repo' -> 'O'
    });

    it('should include all original repository properties', () => {
      const repos = store.repositoriesWithInitials();
      expect(repos[0].id).toBe('repo-1');
      expect(repos[0].name).toBe('test-repo');
      expect(repos[0].path).toBe('/home/user/repos/test-repo');
    });
  });
});
