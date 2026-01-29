import { TestBed, ComponentFixture, fakeAsync, tick } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { signal } from '@angular/core';
import { QuestionAnswerComponent } from './question-answer.component';
import { PendingInputStore } from '../../core/stores/pending-input.store';
import { AgentQuestion, SubmitAnswerDto } from '../../shared/models';

describe('QuestionAnswerComponent', () => {
  let component: QuestionAnswerComponent;
  let fixture: ComponentFixture<QuestionAnswerComponent>;
  let pendingInputStoreMock: {
    questionTimeRemaining: ReturnType<typeof vi.fn>;
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
    timeoutAt: new Date(Date.now() + 300000), // 5 minutes from now
  };

  const mockMultiSelectQuestion: AgentQuestion = {
    ...mockQuestion,
    questions: [
      {
        question: 'Which features do you want?',
        header: 'Features',
        options: [
          { label: 'Feature A', description: 'First feature' },
          { label: 'Feature B', description: 'Second feature' },
          { label: 'Feature C', description: 'Third feature' },
        ],
        multiSelect: true,
      },
    ],
  };

  beforeEach(async () => {
    // Create mock that returns a signal-like function
    pendingInputStoreMock = {
      questionTimeRemaining: vi.fn().mockReturnValue(signal(180)), // 3 minutes
    };

    await TestBed.configureTestingModule({
      imports: [QuestionAnswerComponent],
      providers: [
        {
          provide: PendingInputStore,
          useValue: {
            questionTimeRemaining: signal(180),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(QuestionAnswerComponent);
    component = fixture.componentInstance;

    // Set the required input
    fixture.componentRef.setInput('question', mockQuestion);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display countdown timer', () => {
    const text = fixture.nativeElement.textContent;
    // Timer should be displayed (format: M:SS)
    expect(text).toMatch(/\d+:\d{2}/);
  });

  it('should render all questions from question.questions array', () => {
    const questionText = fixture.nativeElement.textContent;
    expect(questionText).toContain('Which approach do you prefer?');
  });

  it('should render options as radio buttons when multiSelect=false', () => {
    const radios = fixture.nativeElement.querySelectorAll('input[type="radio"]');
    // 2 options + 1 for "Other"
    expect(radios.length).toBe(3);
  });

  it('should render options as checkboxes when multiSelect=true', async () => {
    fixture.componentRef.setInput('question', mockMultiSelectQuestion);
    fixture.detectChanges();
    // Need to reinitialize answers after input change
    component.ngOnInit();
    fixture.detectChanges();

    const checkboxes = fixture.nativeElement.querySelectorAll('input[type="checkbox"]');
    // 3 options + 1 for "Other"
    expect(checkboxes.length).toBe(4);
  });

  it('should show Other text input when selected', () => {
    component.selectOther(0);
    fixture.detectChanges();

    const textInput = fixture.nativeElement.querySelector('input[type="text"]');
    expect(textInput).toBeTruthy();
  });

  it('should disable submit until all questions answered', () => {
    // Initially no answer selected
    expect(component.canSubmit()).toBe(false);

    const submitButton = fixture.nativeElement.querySelector('button[type="button"]:last-of-type');
    expect(submitButton.disabled).toBe(true);
  });

  it('should enable submit when question is answered', () => {
    component.selectSingleOption(0, 0);
    fixture.detectChanges();

    expect(component.canSubmit()).toBe(true);
  });

  it('should emit answered event with correct SubmitAnswerDto', () => {
    const emitSpy = vi.spyOn(component.answered, 'emit');

    // Select first option
    component.selectSingleOption(0, 0);
    fixture.detectChanges();

    // Submit
    component.onSubmit();

    expect(emitSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        answers: [
          expect.objectContaining({
            questionIndex: 0,
            selectedOptionIndices: [0],
          }),
        ],
      })
    );
  });

  describe('option selection', () => {
    it('should select single option for radio buttons', () => {
      component.selectSingleOption(0, 0);
      expect(component.isOptionSelected(0, 0)).toBe(true);
      expect(component.isOptionSelected(0, 1)).toBe(false);

      component.selectSingleOption(0, 1);
      expect(component.isOptionSelected(0, 0)).toBe(false);
      expect(component.isOptionSelected(0, 1)).toBe(true);
    });

    it('should toggle multiple options for checkboxes', async () => {
      fixture.componentRef.setInput('question', mockMultiSelectQuestion);
      fixture.detectChanges();
      component.ngOnInit();

      component.toggleOption(0, 0);
      expect(component.isOptionSelected(0, 0)).toBe(true);

      component.toggleOption(0, 1);
      expect(component.isOptionSelected(0, 0)).toBe(true);
      expect(component.isOptionSelected(0, 1)).toBe(true);

      component.toggleOption(0, 0);
      expect(component.isOptionSelected(0, 0)).toBe(false);
      expect(component.isOptionSelected(0, 1)).toBe(true);
    });
  });

  describe('other/custom answer', () => {
    it('should track other selection state', () => {
      expect(component.isOtherSelected(0)).toBe(false);

      component.selectOther(0);
      expect(component.isOtherSelected(0)).toBe(true);
    });

    it('should clear other selection when regular option selected', () => {
      component.selectOther(0);
      expect(component.isOtherSelected(0)).toBe(true);

      component.selectSingleOption(0, 0);
      expect(component.isOtherSelected(0)).toBe(false);
    });

    it('should store custom answer text', () => {
      component.selectOther(0);
      fixture.detectChanges();

      const event = { target: { value: 'My custom answer' } } as unknown as Event;
      component.onCustomAnswerInput(0, event);

      expect(component.getCustomAnswer(0)).toBe('My custom answer');
    });

    it('should include custom answer in submission', () => {
      const emitSpy = vi.spyOn(component.answered, 'emit');

      component.selectOther(0);
      const event = { target: { value: 'My custom answer' } } as unknown as Event;
      component.onCustomAnswerInput(0, event);
      fixture.detectChanges();

      component.onSubmit();

      expect(emitSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          answers: [
            expect.objectContaining({
              questionIndex: 0,
              selectedOptionIndices: [],
              customAnswer: 'My custom answer',
            }),
          ],
        })
      );
    });
  });

  describe('timer colors', () => {
    it('should show green color when > 2 minutes remaining', () => {
      const classes = component.getTimerClasses();
      expect(classes).toContain('text-green-600');
    });
  });

  describe('formatTimeRemaining', () => {
    it('should format minutes and seconds', () => {
      // The component uses the store's questionTimeRemaining which is mocked to 180
      const formatted = component.formatTimeRemaining();
      expect(formatted).toBe('3:00');
    });
  });
});
