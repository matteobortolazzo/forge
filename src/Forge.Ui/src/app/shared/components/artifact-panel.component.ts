import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Artifact, PIPELINE_STATES, PipelineState } from '../models';
import { ArtifactTypeBadgeComponent } from './artifact-type-badge.component';
import { StateBadgeComponent } from './state-badge.component';

@Component({
  selector: 'app-artifact-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, ArtifactTypeBadgeComponent, StateBadgeComponent],
  template: `
    <div class="flex flex-col h-full">
      <!-- Header -->
      <div class="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700">
        <h3 class="text-sm font-semibold text-gray-900 dark:text-gray-100">
          Artifacts ({{ artifacts().length }})
        </h3>
        @if (artifacts().length > 0) {
          <button
            type="button"
            class="text-xs text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300"
            (click)="toggleExpand()"
          >
            {{ isExpanded() ? 'Collapse' : 'Expand' }}
          </button>
        }
      </div>

      <!-- Content -->
      <div class="flex-1 overflow-y-auto p-3">
        @if (artifacts().length === 0) {
          <p class="text-sm text-gray-500 dark:text-gray-400 italic">
            No artifacts yet. Artifacts will appear here as agents complete their work.
          </p>
        } @else {
          <div class="space-y-3">
            @for (artifact of sortedArtifacts(); track artifact.id) {
              <div
                class="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden"
                [class.ring-2]="selectedArtifactId() === artifact.id"
                [class.ring-blue-500]="selectedArtifactId() === artifact.id"
              >
                <!-- Artifact Header -->
                <button
                  type="button"
                  class="w-full flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800 hover:bg-gray-100 dark:hover:bg-gray-750 transition-colors text-left"
                  (click)="selectArtifact(artifact)"
                  [attr.aria-expanded]="selectedArtifactId() === artifact.id"
                >
                  <div class="flex items-center gap-2">
                    <app-artifact-type-badge [type]="artifact.artifactType" />
                    <app-state-badge [state]="artifact.producedInState" />
                  </div>
                  <span class="text-xs text-gray-500 dark:text-gray-400">
                    {{ artifact.createdAt | date: 'MMM d, h:mm a' }}
                  </span>
                </button>

                <!-- Artifact Content (expandable) -->
                @if (isExpanded() || selectedArtifactId() === artifact.id) {
                  <div class="p-3 bg-white dark:bg-gray-900 border-t border-gray-200 dark:border-gray-700">
                    <pre class="text-xs text-gray-700 dark:text-gray-300 whitespace-pre-wrap font-mono overflow-x-auto max-h-96 overflow-y-auto">{{ artifact.content }}</pre>
                    @if (artifact.agentId) {
                      <p class="mt-2 text-xs text-gray-400 dark:text-gray-500">
                        Agent: {{ artifact.agentId }}
                      </p>
                    }
                  </div>
                }
              </div>
            }
          </div>
        }
      </div>
    </div>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
    }
  `,
})
export class ArtifactPanelComponent {
  readonly artifacts = input.required<Artifact[]>();

  readonly artifactSelected = output<Artifact>();

  readonly isExpanded = signal(false);
  readonly selectedArtifactId = signal<string | null>(null);

  readonly sortedArtifacts = computed(() => {
    return [...this.artifacts()].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
  });

  toggleExpand(): void {
    this.isExpanded.update(v => !v);
    if (!this.isExpanded()) {
      this.selectedArtifactId.set(null);
    }
  }

  selectArtifact(artifact: Artifact): void {
    if (this.selectedArtifactId() === artifact.id) {
      this.selectedArtifactId.set(null);
    } else {
      this.selectedArtifactId.set(artifact.id);
      this.artifactSelected.emit(artifact);
    }
  }
}
