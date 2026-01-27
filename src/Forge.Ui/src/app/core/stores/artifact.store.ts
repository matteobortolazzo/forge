import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Artifact } from '../../shared/models';
import { ArtifactService } from '../services/artifact.service';

@Injectable({ providedIn: 'root' })
export class ArtifactStore {
  private readonly artifactService = inject(ArtifactService);

  // Private signals - internal state
  private readonly artifactsByTaskId = signal<Map<string, Artifact[]>>(new Map());
  private readonly loading = signal(false);
  private readonly error = signal<string | null>(null);

  // Public readonly signals
  readonly isLoading = this.loading.asReadonly();
  readonly errorMessage = this.error.asReadonly();

  /**
   * Gets artifacts for a specific task.
   */
  getArtifactsForTask(taskId: string): Artifact[] {
    return this.artifactsByTaskId().get(taskId) ?? [];
  }

  /**
   * Gets the count of artifacts for a task.
   */
  getArtifactCount(taskId: string): number {
    return this.getArtifactsForTask(taskId).length;
  }

  /**
   * Gets the latest artifact for a task.
   */
  getLatestArtifact(taskId: string): Artifact | undefined {
    const artifacts = this.getArtifactsForTask(taskId);
    if (artifacts.length === 0) return undefined;
    return artifacts.reduce((latest, current) =>
      new Date(current.createdAt) > new Date(latest.createdAt) ? current : latest
    );
  }

  /**
   * Loads artifacts for a specific task.
   */
  async loadArtifactsForTask(taskId: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const artifacts = await firstValueFrom(this.artifactService.getArtifactsForTask(taskId));
      this.artifactsByTaskId.update(map => {
        const newMap = new Map(map);
        newMap.set(taskId, artifacts);
        return newMap;
      });
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to load artifacts');
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Adds an artifact from an SSE event.
   */
  addArtifactFromEvent(artifact: Artifact): void {
    this.artifactsByTaskId.update(map => {
      const newMap = new Map(map);
      const existingArtifacts = newMap.get(artifact.taskId) ?? [];
      // Check if artifact already exists
      if (!existingArtifacts.some(a => a.id === artifact.id)) {
        newMap.set(artifact.taskId, [...existingArtifacts, artifact]);
      }
      return newMap;
    });
  }

  /**
   * Clears artifacts for a task (e.g., when navigating away).
   */
  clearArtifactsForTask(taskId: string): void {
    this.artifactsByTaskId.update(map => {
      const newMap = new Map(map);
      newMap.delete(taskId);
      return newMap;
    });
  }

  /**
   * Clears all cached artifacts.
   */
  clearAll(): void {
    this.artifactsByTaskId.set(new Map());
  }
}
