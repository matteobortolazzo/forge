import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay } from 'rxjs';
import { RepositoryInfo } from '../../shared/models';

@Injectable({ providedIn: 'root' })
export class RepositoryService {
  private readonly http = inject(HttpClient);
  private readonly useMocks = false;
  private readonly apiUrl = '/api/repository';

  // Mock data for offline development
  private readonly mockInfo: RepositoryInfo = {
    name: 'forge',
    path: '/home/user/repos/forge',
    branch: 'feature/repo-info',
    commitHash: 'abc1234',
    remoteUrl: 'git@github.com:user/forge.git',
    isDirty: true,
    isGitRepository: true,
  };

  getInfo(): Observable<RepositoryInfo> {
    if (this.useMocks) {
      return of({ ...this.mockInfo }).pipe(delay(200));
    }
    return this.http.get<RepositoryInfo>(`${this.apiUrl}/info`);
  }
}
