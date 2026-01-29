import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { signal } from '@angular/core';
import { PendingInputPanelComponent } from './pending-input-panel.component';
import { PendingInputStore } from '../../core/stores/pending-input.store';
import { HumanGate, AgentQuestion, PendingInputItem } from '../../shared/models';

describe('PendingInputPanelComponent', () => {
  let component: PendingInputPanelComponent;
  let fixture: ComponentFixture<PendingInputPanelComponent>;
  let pendingInputStoreMock: {
    gates: ReturnType<typeof signal>;
    question: ReturnType<typeof signal>;
    pendingCount: ReturnType<typeof signal>;
    allPendingItems: ReturnType<typeof signal>;
    hasUrgentInput: ReturnType<typeof signal>;
    questionTimeRemaining: ReturnType<typeof signal>;
    resolveGate: ReturnType<typeof vi.fn>;
    answerQuestion: ReturnType<typeof vi.fn>;
  };

  const mockGate: HumanGate = {
    id: 'gate-1',
    taskId: 'task-1',
    gateType: 'planning',
    status: 'pending',
    confidenceScore: 0.65,
    reason: 'Low confidence in implementation approach',
    requestedAt: new Date('2026-01-29T10:00:00Z'),
  };

  const mockQuestion: AgentQuestion = {
    id: 'question-1',
    taskId: 'task-1',
    toolUseId: 'tool-use-123',
    questions: [
      {
        question: 'Which approach do you prefer?',
        header: 'Approach',
        options: [
          { label: 'Option A', description: 'First approach' },
          { label: 'Option B', description: 'Second approach' },
        ],
        multiSelect: false,
      },
    ],
    status: 'pending',
    requestedAt: new Date('2026-01-29T10:00:00Z'),
    timeoutAt: new Date(Date.now() + 300000),
  };

  beforeEach(async () => {
    pendingInputStoreMock = {
      gates: signal<HumanGate[]>([]),
      question: signal<AgentQuestion | null>(null),
      pendingCount: signal(0),
      allPendingItems: signal<PendingInputItem[]>([]),
      hasUrgentInput: signal(false),
      questionTimeRemaining: signal<number | null>(null),
      resolveGate: vi.fn().mockResolvedValue(true),
      answerQuestion: vi.fn().mockResolvedValue(true),
    };

    await TestBed.configureTestingModule({
      imports: [PendingInputPanelComponent],
      providers: [
        { provide: PendingInputStore, useValue: pendingInputStoreMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PendingInputPanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should not show badge when pendingCount is 0', () => {
    const badge = fixture.nativeElement.querySelector('span[aria-label]');
    expect(badge).toBeNull();
  });

  it('should show count when items pending', () => {
    pendingInputStoreMock.pendingCount.set(3);
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('span[aria-label="3 pending items"]');
    expect(badge).toBeTruthy();
    expect(badge.textContent.trim()).toBe('3');
  });

  it('should show 99+ when count exceeds 99', () => {
    pendingInputStoreMock.pendingCount.set(150);
    fixture.detectChanges();

    // Find the badge (it has the count)
    const badges: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('span');
    const countBadge = Array.from(badges).find(b => b.textContent?.includes('99+'));
    expect(countBadge).toBeTruthy();
  });

  it('should have amber styling for gates only', () => {
    pendingInputStoreMock.gates.set([mockGate]);
    pendingInputStoreMock.pendingCount.set(1);
    pendingInputStoreMock.hasUrgentInput.set(false);
    fixture.detectChanges();

    const classes = component.getBadgeClasses();
    expect(classes).toContain('bg-amber-500');
    expect(classes).not.toContain('animate-pulse');
  });

  it('should have red styling with pulse when question pending', () => {
    pendingInputStoreMock.question.set(mockQuestion);
    pendingInputStoreMock.pendingCount.set(1);
    pendingInputStoreMock.hasUrgentInput.set(true);
    fixture.detectChanges();

    const classes = component.getBadgeClasses();
    expect(classes).toContain('bg-red-500');
    expect(classes).toContain('animate-pulse');
  });

  it('should toggle panel on click', () => {
    expect(component.isPanelOpen()).toBe(false);

    component.togglePanel();
    fixture.detectChanges();

    expect(component.isPanelOpen()).toBe(true);

    component.togglePanel();
    fixture.detectChanges();

    expect(component.isPanelOpen()).toBe(false);
  });

  describe('panel content', () => {
    beforeEach(() => {
      component.isPanelOpen.set(true);
      fixture.detectChanges();
    });

    it('should show empty state when no pending items', () => {
      const emptyState = fixture.nativeElement.textContent;
      expect(emptyState).toContain('No pending input required');
    });

    it('should show question section when question exists', () => {
      pendingInputStoreMock.question.set(mockQuestion);
      pendingInputStoreMock.pendingCount.set(1);
      pendingInputStoreMock.hasUrgentInput.set(true);
      fixture.detectChanges();

      const questionComponent = fixture.nativeElement.querySelector('app-question-answer');
      expect(questionComponent).toBeTruthy();
    });

    it('should show gates section when gates exist', () => {
      pendingInputStoreMock.gates.set([mockGate]);
      pendingInputStoreMock.pendingCount.set(1);
      fixture.detectChanges();

      const text = fixture.nativeElement.textContent;
      expect(text).toContain('Human Gates (1)');
    });

    it('should prioritize question display above gates', () => {
      pendingInputStoreMock.question.set(mockQuestion);
      pendingInputStoreMock.gates.set([mockGate]);
      pendingInputStoreMock.pendingCount.set(2);
      pendingInputStoreMock.hasUrgentInput.set(true);
      fixture.detectChanges();

      const content = fixture.nativeElement.querySelector('.max-h-\\[32rem\\]');
      const questionSection = content.querySelector('app-question-answer');
      const gateSection = content.querySelector('app-gate-resolution');

      // Both should exist
      expect(questionSection).toBeTruthy();
      expect(gateSection).toBeTruthy();

      // Question should come before gates (check DOM order)
      const allElements = content.querySelectorAll('app-question-answer, app-gate-resolution');
      expect(allElements[0].tagName.toLowerCase()).toBe('app-question-answer');
    });
  });

  describe('keyboard handling', () => {
    it('should close on escape key', () => {
      component.isPanelOpen.set(true);
      fixture.detectChanges();

      component.closePanel();
      fixture.detectChanges();

      expect(component.isPanelOpen()).toBe(false);
    });
  });

  describe('click outside handling', () => {
    it('should close on click outside', () => {
      component.isPanelOpen.set(true);
      fixture.detectChanges();

      // Simulate click outside
      const mockEvent = {
        target: document.createElement('div'),
      } as unknown as MouseEvent;
      (mockEvent.target as HTMLElement).closest = vi.fn().mockReturnValue(null);

      component.onDocumentClick(mockEvent);

      expect(component.isPanelOpen()).toBe(false);
    });

    it('should not close on click inside', () => {
      component.isPanelOpen.set(true);
      fixture.detectChanges();

      // Simulate click inside - the element has closest that returns truthy for app-pending-input-panel
      const insideElement = document.createElement('div');
      insideElement.closest = vi.fn().mockReturnValue(fixture.nativeElement);
      const mockEvent = {
        target: insideElement,
      } as unknown as MouseEvent;

      component.onDocumentClick(mockEvent);

      expect(component.isPanelOpen()).toBe(true);
    });
  });

  describe('button classes', () => {
    it('should have gray classes when no pending items', () => {
      const classes = component.getBadgeButtonClasses();
      expect(classes).toContain('text-gray-500');
    });

    it('should have amber classes for gates only', () => {
      pendingInputStoreMock.gates.set([mockGate]);
      pendingInputStoreMock.hasUrgentInput.set(false);
      fixture.detectChanges();

      const classes = component.getBadgeButtonClasses();
      expect(classes).toContain('text-amber-500');
    });

    it('should have red classes when question pending', () => {
      pendingInputStoreMock.question.set(mockQuestion);
      pendingInputStoreMock.hasUrgentInput.set(true);
      fixture.detectChanges();

      const classes = component.getBadgeButtonClasses();
      expect(classes).toContain('text-red-500');
    });
  });
});
