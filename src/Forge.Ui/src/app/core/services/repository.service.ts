import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay } from 'rxjs';
import { Repository, CreateRepositoryDto, UpdateRepositoryDto } from '../../shared/models';

@Injectable({ providedIn: 'root' })
export class RepositoryService {
  private readonly http = inject(HttpClient);
  private readonly useMocks = false;
  private readonly apiUrl = '/api/repositories';

  // Mock data for offline development
  private mockRepositories: Repository[] = [
    {
      id: 'repo-1',
      name: 'forge',
      path: '/home/user/repos/forge',
      isActive: true,
      branch: 'feature/repo-info',
      commitHash: 'abc1234',
      remoteUrl: 'git@github.com:user/forge.git',
      isDirty: true,
      isGitRepository: true,
      lastRefreshedAt: new Date(),
      createdAt: new Date(),
      updatedAt: new Date(),
      taskCount: 5,
    },
  ];

  getAll(): Observable<Repository[]> {
    if (this.useMocks) {
      return of([...this.mockRepositories]).pipe(delay(200));
    }
    return this.http.get<Repository[]>(this.apiUrl);
  }

  getById(id: string): Observable<Repository> {
    if (this.useMocks) {
      const repo = this.mockRepositories.find(r => r.id === id);
      if (repo) {
        return of({ ...repo }).pipe(delay(200));
      }
      throw new Error('Repository not found');
    }
    return this.http.get<Repository>(`${this.apiUrl}/${id}`);
  }

  create(dto: CreateRepositoryDto): Observable<Repository> {
    if (this.useMocks) {
      const newRepo: Repository = {
        id: `repo-${Date.now()}`,
        name: dto.name,
        path: dto.path,
        isActive: true,
        isGitRepository: true,
        createdAt: new Date(),
        updatedAt: new Date(),
        taskCount: 0,
      };
      // Add to beginning (newest first)
      this.mockRepositories.unshift(newRepo);
      return of({ ...newRepo }).pipe(delay(300));
    }
    return this.http.post<Repository>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateRepositoryDto): Observable<Repository> {
    if (this.useMocks) {
      const index = this.mockRepositories.findIndex(r => r.id === id);
      if (index === -1) {
        throw new Error('Repository not found');
      }
      const updated: Repository = {
        ...this.mockRepositories[index],
        ...dto,
        updatedAt: new Date(),
      };
      this.mockRepositories[index] = updated;
      return of({ ...updated }).pipe(delay(200));
    }
    return this.http.patch<Repository>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: string): Observable<void> {
    if (this.useMocks) {
      this.mockRepositories = this.mockRepositories.filter(r => r.id !== id);
      return of(undefined).pipe(delay(200));
    }
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  refresh(id: string): Observable<Repository> {
    if (this.useMocks) {
      const index = this.mockRepositories.findIndex(r => r.id === id);
      if (index === -1) {
        throw new Error('Repository not found');
      }
      const updated: Repository = {
        ...this.mockRepositories[index],
        lastRefreshedAt: new Date(),
        updatedAt: new Date(),
      };
      this.mockRepositories[index] = updated;
      return of({ ...updated }).pipe(delay(500));
    }
    return this.http.post<Repository>(`${this.apiUrl}/${id}/refresh`, {});
  }

  // Legacy method for backward compatibility
  getInfo(): Observable<Repository> {
    if (this.useMocks) {
      // Return first repository
      return of(this.mockRepositories[0] ? { ...this.mockRepositories[0] } : this.mockRepositories[0]).pipe(delay(200));
    }
    // Get first repository
    return this.http.get<Repository[]>(this.apiUrl).pipe(
      delay(0), // Don't actually delay for this derived observable
    ) as unknown as Observable<Repository>;
  }
}
