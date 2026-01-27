import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of, delay } from 'rxjs';
import { Artifact } from '../../shared/models';

@Injectable({ providedIn: 'root' })
export class ArtifactService {
  private readonly http = inject(HttpClient);
  private readonly useMocks = false;

  /**
   * Gets all artifacts for a task.
   */
  getArtifactsForTask(taskId: string): Observable<Artifact[]> {
    if (this.useMocks) {
      return of([]).pipe(delay(200));
    }
    return this.http.get<Artifact[]>(`/api/tasks/${taskId}/artifacts`);
  }

  /**
   * Gets a specific artifact by ID.
   */
  getArtifact(taskId: string, artifactId: string): Observable<Artifact> {
    if (this.useMocks) {
      return of({
        id: artifactId,
        taskId,
        producedInState: 'Planning',
        artifactType: 'plan',
        content: '# Mock Plan\n\nThis is a mock artifact.',
        createdAt: new Date(),
      } as Artifact).pipe(delay(200));
    }
    return this.http.get<Artifact>(`/api/tasks/${taskId}/artifacts/${artifactId}`);
  }

  /**
   * Gets the latest artifact for a task.
   */
  getLatestArtifact(taskId: string): Observable<Artifact> {
    if (this.useMocks) {
      return of({
        id: 'mock-latest',
        taskId,
        producedInState: 'Planning',
        artifactType: 'plan',
        content: '# Mock Latest Plan\n\nThis is the latest mock artifact.',
        createdAt: new Date(),
      } as Artifact).pipe(delay(200));
    }
    return this.http.get<Artifact>(`/api/tasks/${taskId}/artifacts/latest`);
  }

  /**
   * Gets artifacts by pipeline state.
   */
  getArtifactsByState(taskId: string, state: string): Observable<Artifact[]> {
    if (this.useMocks) {
      return of([]).pipe(delay(200));
    }
    return this.http.get<Artifact[]>(`/api/tasks/${taskId}/artifacts/by-state/${state}`);
  }
}
