import { TestBed } from '@angular/core/testing';
import { RepositoryStore } from './repository.store';
import { RepositoryService } from '../services/repository.service';
import { of, throwError } from 'rxjs';
import { RepositoryInfo } from '../../shared/models';
import { describe, it, expect, beforeEach, vi } from 'vitest';

describe('RepositoryStore', () => {
  let store: RepositoryStore;
  let repositoryServiceMock: { getInfo: ReturnType<typeof vi.fn> };

  const mockRepositoryInfo: RepositoryInfo = {
    name: 'test-repo',
    path: '/home/user/repos/test-repo',
    branch: 'main',
    commitHash: 'abc1234',
    remoteUrl: 'git@github.com:user/test-repo.git',
    isDirty: false,
    isGitRepository: true,
  };

  beforeEach(() => {
    repositoryServiceMock = {
      getInfo: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        RepositoryStore,
        { provide: RepositoryService, useValue: repositoryServiceMock },
      ],
    });

    store = TestBed.inject(RepositoryStore);
  });

  it('should be created', () => {
    expect(store).toBeTruthy();
  });

  describe('loadInfo', () => {
    it('should set info signal on success', async () => {
      repositoryServiceMock.getInfo.mockReturnValue(of(mockRepositoryInfo));

      await store.loadInfo();

      expect(store.info()).toEqual(mockRepositoryInfo);
    });

    it('should set loading to false after fetch completes', async () => {
      repositoryServiceMock.getInfo.mockReturnValue(of(mockRepositoryInfo));

      await store.loadInfo();

      expect(store.loading()).toBe(false);
    });

    it('should set error on failure', async () => {
      const errorMessage = 'Network error';
      repositoryServiceMock.getInfo.mockReturnValue(throwError(() => new Error(errorMessage)));

      await store.loadInfo();

      expect(store.error()).toBe(errorMessage);
      expect(store.info()).toBeNull();
    });

    it('should set loading to false after error', async () => {
      repositoryServiceMock.getInfo.mockReturnValue(throwError(() => new Error('Error')));

      await store.loadInfo();

      expect(store.loading()).toBe(false);
    });
  });

  describe('computed signals', () => {
    beforeEach(async () => {
      repositoryServiceMock.getInfo.mockReturnValue(of(mockRepositoryInfo));
      await store.loadInfo();
    });

    it('should return name from info', () => {
      expect(store.name()).toBe('test-repo');
    });

    it('should return path from info', () => {
      expect(store.path()).toBe('/home/user/repos/test-repo');
    });

    it('should return branch from info', () => {
      expect(store.branch()).toBe('main');
    });

    it('should return commitHash from info', () => {
      expect(store.commitHash()).toBe('abc1234');
    });

    it('should return remoteUrl from info', () => {
      expect(store.remoteUrl()).toBe('git@github.com:user/test-repo.git');
    });

    it('should return isDirty from info', () => {
      expect(store.isDirty()).toBe(false);
    });

    it('should return isGitRepository from info', () => {
      expect(store.isGitRepository()).toBe(true);
    });
  });

  describe('displayBranch', () => {
    it('should return null when no branch', async () => {
      const infoWithoutBranch: RepositoryInfo = {
        ...mockRepositoryInfo,
        branch: undefined,
      };
      repositoryServiceMock.getInfo.mockReturnValue(of(infoWithoutBranch));
      await store.loadInfo();

      expect(store.displayBranch()).toBeNull();
    });

    it('should return full branch name when under 30 chars', async () => {
      const shortBranchInfo: RepositoryInfo = {
        ...mockRepositoryInfo,
        branch: 'feature/short-name',
      };
      repositoryServiceMock.getInfo.mockReturnValue(of(shortBranchInfo));
      await store.loadInfo();

      expect(store.displayBranch()).toBe('feature/short-name');
    });

    it('should truncate long branch names', async () => {
      const longBranchInfo: RepositoryInfo = {
        ...mockRepositoryInfo,
        branch: 'feature/this-is-a-very-long-branch-name-that-exceeds-thirty-characters',
      };
      repositoryServiceMock.getInfo.mockReturnValue(of(longBranchInfo));
      await store.loadInfo();

      const displayBranch = store.displayBranch();
      expect(displayBranch).not.toBeNull();
      expect(displayBranch!.length).toBe(30);
      expect(displayBranch).toMatch(/\.\.\.$/);
    });
  });

  describe('default values before loading', () => {
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
});
