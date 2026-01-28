import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of, delay } from 'rxjs';
import { Artifact } from '../../shared/models';

@Injectable({ providedIn: 'root' })
export class ArtifactService {
  private readonly http = inject(HttpClient);
  private readonly useMocks = false;

  private getTaskUrl(repositoryId: string, backlogItemId: string, taskId: string): string {
    return `/api/repositories/${repositoryId}/backlog/${backlogItemId}/tasks/${taskId}`;
  }

  /**
   * Gets all artifacts for a task.
   */
  getArtifactsForTask(
    repositoryId: string,
    backlogItemId: string,
    taskId: string
  ): Observable<Artifact[]> {
    if (this.useMocks) {
      return of([]).pipe(delay(200));
    }
    return this.http.get<Artifact[]>(
      `${this.getTaskUrl(repositoryId, backlogItemId, taskId)}/artifacts`
    );
  }

  /**
   * Gets a specific artifact by ID.
   */
  getArtifact(
    repositoryId: string,
    backlogItemId: string,
    taskId: string,
    artifactId: string
  ): Observable<Artifact> {
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
    return this.http.get<Artifact>(
      `${this.getTaskUrl(repositoryId, backlogItemId, taskId)}/artifacts/${artifactId}`
    );
  }

  /**
   * Gets the latest artifact for a task.
   */
  getLatestArtifact(
    repositoryId: string,
    backlogItemId: string,
    taskId: string
  ): Observable<Artifact> {
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
    return this.http.get<Artifact>(
      `${this.getTaskUrl(repositoryId, backlogItemId, taskId)}/artifacts/latest`
    );
  }

  /**
   * Gets artifacts by pipeline state.
   */
  getArtifactsByState(
    repositoryId: string,
    backlogItemId: string,
    taskId: string,
    state: string
  ): Observable<Artifact[]> {
    if (this.useMocks) {
      return of([]).pipe(delay(200));
    }
    return this.http.get<Artifact[]>(
      `${this.getTaskUrl(repositoryId, backlogItemId, taskId)}/artifacts/by-state/${state}`
    );
  }
}
