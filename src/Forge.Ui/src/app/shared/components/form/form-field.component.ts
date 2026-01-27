import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { LABEL_CLASSES, ERROR_CLASSES } from './form-classes';

/**
 * A form field wrapper that provides consistent label and error styling.
 * Use ng-content to project the actual input/textarea/select element.
 *
 * @example
 * <app-form-field id="title" label="Title" [error]="form.controls.title.invalid ? 'Required' : ''">
 *   <input id="title" type="text" formControlName="title" [class]="inputClasses" />
 * </app-form-field>
 */
@Component({
  selector: 'app-form-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div>
      <label [for]="id()" [class]="labelClasses">
        {{ label() }}
      </label>
      <ng-content />
      @if (error()) {
        <p [class]="errorClasses">{{ error() }}</p>
      }
    </div>
  `,
})
export class FormFieldComponent {
  /** The id of the input element (used for label's for attribute) */
  readonly id = input.required<string>();

  /** The label text */
  readonly label = input.required<string>();

  /** Error message to display (empty string or undefined hides error) */
  readonly error = input<string>('');

  readonly labelClasses = LABEL_CLASSES;
  readonly errorClasses = ERROR_CLASSES;
}
