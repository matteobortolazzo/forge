import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { signal } from '@angular/core';
import { GateResolutionComponent } from './gate-resolution.component';
import { HumanGate, ResolveGateDto } from '../../shared/models';

describe('GateResolutionComponent', () => {
  let component: GateResolutionComponent;
  let fixture: ComponentFixture<GateResolutionComponent>;

  const mockGate: HumanGate = {
    id: 'gate-1',
    taskId: 'task-1',
    gateType: 'planning',
    status: 'pending',
    confidenceScore: 0.65,
    reason: 'Low confidence in implementation approach',
    requestedAt: new Date('2026-01-29T10:00:00Z'),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GateResolutionComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(GateResolutionComponent);
    component = fixture.componentInstance;

    // Set the required input
    fixture.componentRef.setInput('gate', mockGate);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display gate type badge', () => {
    const badge = fixture.nativeElement.querySelector('span');
    expect(badge.textContent.trim()).toBe('Planning');
  });

  it('should display confidence score', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('65%');
  });

  it('should display reason text', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Low confidence in implementation approach');
  });

  it('should emit resolved event with Approved status on approve click', () => {
    const emitSpy = vi.spyOn(component.resolved, 'emit');

    const approveButton = fixture.nativeElement.querySelector('button');
    approveButton.click();

    expect(emitSpy).toHaveBeenCalledWith(
      expect.objectContaining({ status: 'approved' })
    );
  });

  it('should emit resolved event with Rejected status on reject click', () => {
    const emitSpy = vi.spyOn(component.resolved, 'emit');

    const buttons = fixture.nativeElement.querySelectorAll('button');
    const rejectButton = buttons[1]; // Second button is Reject
    rejectButton.click();

    expect(emitSpy).toHaveBeenCalledWith(
      expect.objectContaining({ status: 'rejected' })
    );
  });

  it('should emit resolved event with Skipped status on skip click', () => {
    const emitSpy = vi.spyOn(component.resolved, 'emit');

    const buttons = fixture.nativeElement.querySelectorAll('button');
    const skipButton = buttons[2]; // Third button is Skip
    skipButton.click();

    expect(emitSpy).toHaveBeenCalledWith(
      expect.objectContaining({ status: 'skipped' })
    );
  });

  it('should include resolution message when provided', () => {
    // Toggle resolution input
    component.toggleResolutionInput();
    fixture.detectChanges();

    // Set message
    component.resolutionMessage.set('Test resolution message');

    const emitSpy = vi.spyOn(component.resolved, 'emit');

    const approveButton = fixture.nativeElement.querySelector('button');
    approveButton.click();

    expect(emitSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        status: 'approved',
        resolution: 'Test resolution message',
      })
    );
  });

  it('should toggle resolution input visibility', () => {
    expect(component.showResolutionInput()).toBe(false);

    component.toggleResolutionInput();
    expect(component.showResolutionInput()).toBe(true);

    component.toggleResolutionInput();
    expect(component.showResolutionInput()).toBe(false);
  });

  describe('formatGateType', () => {
    it('should format refining type', () => {
      expect(component.formatGateType('refining')).toBe('Refining');
    });

    it('should format split type', () => {
      expect(component.formatGateType('split')).toBe('Split');
    });

    it('should format planning type', () => {
      expect(component.formatGateType('planning')).toBe('Planning');
    });

    it('should return raw type for unknown gate types', () => {
      expect(component.formatGateType('unknown')).toBe('unknown');
    });
  });

  describe('gate type badge colors', () => {
    it('should apply blue classes for planning gate', () => {
      const classes = component.getGateTypeBadgeClasses();
      expect(classes).toContain('bg-blue-100');
    });

    it('should apply amber classes for split gate', () => {
      fixture.componentRef.setInput('gate', { ...mockGate, gateType: 'split' });
      fixture.detectChanges();

      const classes = component.getGateTypeBadgeClasses();
      expect(classes).toContain('bg-amber-100');
    });

    it('should apply green classes for refining gate', () => {
      fixture.componentRef.setInput('gate', { ...mockGate, gateType: 'refining' });
      fixture.detectChanges();

      const classes = component.getGateTypeBadgeClasses();
      expect(classes).toContain('bg-green-100');
    });
  });
});
