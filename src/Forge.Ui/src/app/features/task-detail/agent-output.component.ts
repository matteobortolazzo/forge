import { Component, ChangeDetectionStrategy, input, computed, ElementRef, viewChild, effect } from '@angular/core';
import { TaskLog, LogType } from '../../shared/models';

@Component({
  selector: 'app-agent-output',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex h-full flex-col rounded-lg bg-gray-900 font-mono text-sm">
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-gray-700 px-4 py-2">
        <h3 class="text-sm font-medium text-gray-300">Agent Output</h3>
        <span class="text-xs text-gray-500">
          {{ logs().length }} entries
        </span>
      </div>

      <!-- Log Container -->
      <div
        #logContainer
        class="flex-1 overflow-y-auto p-4"
        role="log"
        aria-live="polite"
        aria-label="Agent output log"
      >
        @if (logs().length === 0) {
          <p class="text-center text-gray-500">No agent activity yet</p>
        } @else {
          <div class="space-y-2">
            @for (log of logs(); track log.id) {
              <div [class]="getLogClasses(log.type)">
                <div class="flex items-start gap-2">
                  <!-- Icon/Indicator -->
                  <span [class]="getIconClasses(log.type)" aria-hidden="true">
                    @switch (log.type) {
                      @case ('thinking') {
                        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                          <path d="M10 3.5a1.5 1.5 0 013 0V4a1 1 0 001 1h3a1 1 0 011 1v3a1 1 0 01-1 1h-.5a1.5 1.5 0 000 3h.5a1 1 0 011 1v3a1 1 0 01-1 1h-3a1 1 0 01-1-1v-.5a1.5 1.5 0 00-3 0v.5a1 1 0 01-1 1H6a1 1 0 01-1-1v-3a1 1 0 00-1-1h-.5a1.5 1.5 0 010-3H4a1 1 0 001-1V6a1 1 0 011-1h3a1 1 0 001-1v-.5z" />
                        </svg>
                      }
                      @case ('toolUse') {
                        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm.75-11.25a.75.75 0 00-1.5 0v2.5h-2.5a.75.75 0 000 1.5h2.5v2.5a.75.75 0 001.5 0v-2.5h2.5a.75.75 0 000-1.5h-2.5v-2.5z" clip-rule="evenodd" />
                        </svg>
                      }
                      @case ('toolResult') {
                        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
                        </svg>
                      }
                      @case ('error') {
                        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z" clip-rule="evenodd" />
                        </svg>
                      }
                      @default {
                        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
                        </svg>
                      }
                    }
                  </span>

                  <!-- Content -->
                  <div class="flex-1 min-w-0">
                    @if (log.toolName) {
                      <span class="text-xs font-medium text-gray-500">
                        [{{ log.toolName }}]
                      </span>
                    }
                    <pre class="whitespace-pre-wrap break-words">{{ log.content }}</pre>
                    <span class="mt-1 block text-xs text-gray-600">
                      {{ formatTime(log.timestamp) }}
                    </span>
                  </div>
                </div>
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
    pre {
      font-family: ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, monospace;
      margin: 0;
    }
  `,
})
export class AgentOutputComponent {
  readonly logs = input.required<TaskLog[]>();
  readonly autoScroll = input(true);

  private readonly logContainer = viewChild<ElementRef<HTMLDivElement>>('logContainer');

  constructor() {
    // Auto-scroll to bottom when new logs arrive
    effect(() => {
      const logs = this.logs();
      const container = this.logContainer();
      if (this.autoScroll() && container && logs.length > 0) {
        setTimeout(() => {
          container.nativeElement.scrollTop = container.nativeElement.scrollHeight;
        }, 0);
      }
    });
  }

  getLogClasses(type: LogType): string {
    const base = 'rounded p-2';
    switch (type) {
      case 'thinking':
        return `${base} bg-purple-900/30 text-purple-300 border-l-2 border-purple-500`;
      case 'toolUse':
        return `${base} bg-blue-900/30 text-blue-300 border-l-2 border-blue-500`;
      case 'toolResult':
        return `${base} bg-green-900/30 text-green-300 border-l-2 border-green-500`;
      case 'error':
        return `${base} bg-red-900/30 text-red-300 border-l-2 border-red-500`;
      default:
        return `${base} bg-gray-800 text-gray-300 border-l-2 border-gray-600`;
    }
  }

  getIconClasses(type: LogType): string {
    const base = 'flex-shrink-0 mt-0.5';
    switch (type) {
      case 'thinking':
        return `${base} text-purple-400`;
      case 'toolUse':
        return `${base} text-blue-400`;
      case 'toolResult':
        return `${base} text-green-400`;
      case 'error':
        return `${base} text-red-400`;
      default:
        return `${base} text-gray-400`;
    }
  }

  formatTime(date: Date): string {
    return new Date(date).toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  }
}
