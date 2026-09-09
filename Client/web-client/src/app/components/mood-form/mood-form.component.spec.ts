import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatButtonModule } from '@angular/material/button';

import { MoodFormComponent } from './mood-form.component';
import { MoodService } from '../../services/mood.service';

import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';

import {
  CreateMoodEntryResponse,
  MoodRating
} from '../../models/mood-entry';

describe('MoodFormComponent', () => {
  let component: MoodFormComponent;
  let fixture: ComponentFixture<MoodFormComponent>;
  let moodService: jasmine.SpyObj<MoodService>;

  beforeEach(async () => {
    moodService = jasmine.createSpyObj<MoodService>(
      'MoodService',
      ['create']
    );

    await TestBed.configureTestingModule({
      declarations: [MoodFormComponent],
      imports: [
        ReactiveFormsModule,
        NoopAnimationsModule,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatRadioModule,
        MatButtonModule
      ],
      providers: [
        { provide: MoodService, useValue: moodService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MoodFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should show all four mood choices', () => {
    // Arrange
    const element = fixture.nativeElement as HTMLElement;

    // Act
    const choices = element.querySelectorAll('mat-radio-button');

    // Assert
    expect(choices.length).toBe(4);
    expect(element.textContent).toContain('Not good at all');
    expect(element.textContent).toContain('A bit “meh”');
    expect(element.textContent).toContain('Pretty good');
    expect(element.textContent).toContain('Feeling great');
  });

  it('should reject an empty form without calling the service', () => {
    // Arrange
    component.form.reset();

    // Act
    component.submit();
    fixture.detectChanges();

    // Assert
    expect(moodService.create).not.toHaveBeenCalled();
    expect(component.form.controls.employeeIdentifier.touched).toBeTrue();
    expect(component.form.controls.rating.touched).toBeTrue();
    expect(component.isSubmitting).toBeFalse();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Select a mood.');
  });

  it('should submit normalized values and show confirmation', () => {
    // Arrange
    component.form.setValue({
      employeeIdentifier: '  EMP001  ',
      rating: MoodRating.PrettyGood,
      comment: '  Good morning.  '
    });

    moodService.create.and.returnValue(of({
      entryId: 1,
      message: 'Your mood has been recorded.'
    }));

    // Act
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', {
      bubbles: true,
      cancelable: true
    }));
    fixture.detectChanges();

    // Assert
    expect(moodService.create).toHaveBeenCalledOnceWith({
      employeeIdentifier: 'EMP001',
      rating: MoodRating.PrettyGood,
      comment: 'Good morning.'
    });

    expect(component.form.getRawValue()).toEqual({
      employeeIdentifier: '',
      rating: null,
      comment: ''
    });

    expect(component.isSubmitting).toBeFalse();
    expect(component.form.enabled).toBeTrue();

    const message = fixture.nativeElement.querySelector('[role="status"]');
    expect(message?.textContent).toContain('Your mood has been recorded.');
    expect(fixture.nativeElement.querySelector('mat-error')).toBeNull();
    expect(component.form.controls.employeeIdentifier.hasError('required')).toBeTrue();
  });

  it('should submit without an optional comment', () => {
    // Arrange
    component.form.setValue({
      employeeIdentifier: 'EMP001',
      rating: MoodRating.FeelingGreat,
      comment: ''
    });

    moodService.create.and.returnValue(of({
      entryId: 2,
      message: 'Your mood has been recorded.'
    }));

    // Act
    component.submit();

    // Assert
    expect(moodService.create).toHaveBeenCalledOnceWith({
      employeeIdentifier: 'EMP001',
      rating: MoodRating.FeelingGreat,
      comment: undefined
    });
  });

  it('should disable controls and prevent repeated submissions while loading', () => {
    // Arrange
    component.form.setValue({
      employeeIdentifier: 'EMP001',
      rating: MoodRating.PrettyGood,
      comment: ''
    });

    const response = new Subject<CreateMoodEntryResponse>();
    moodService.create.and.returnValue(response);

    // Act
    component.submit();
    component.submit();
    fixture.detectChanges();

    // Assert
    expect(moodService.create).toHaveBeenCalledTimes(1);
    expect(component.isSubmitting).toBeTrue();
    expect(component.form.disabled).toBeTrue();

    const button = fixture.nativeElement
      .querySelector('button[type="submit"]') as HTMLButtonElement;

    expect(button.disabled).toBeTrue();
    expect(button.textContent).toContain('Submitting');

    // Act: complete the pending request
    response.next({
      entryId: 3,
      message: 'Your mood has been recorded.'
    });
    response.complete();
    fixture.detectChanges();

    // Assert
    expect(component.isSubmitting).toBeFalse();
    expect(component.form.enabled).toBeTrue();
    expect(button.disabled).toBeFalse();
  });

  const errorCases = [
    {
      status: 400,
      body: { detail: 'Enter a valid employee ID.' },
      expected: 'Enter a valid employee ID.'
    },
    {
      status: 403,
      body: { detail: 'This employee is not active.' },
      expected: 'This employee is not active.'
    },
    {
      status: 409,
      body: { detail: 'You have already recorded your mood today.' },
      expected: 'You have already recorded your mood today.'
    },
    {
      status: 0,
      body: null,
      expected: 'Unable to reach the server. Please try again.'
    },
    {
      status: 500,
      body: { detail: 'Internal database details should not be displayed.' },
      expected: 'Something went wrong. Please try again later.'
    },
    {
      status: 400,
      body: {},
      expected: 'Please check your details and try again.'
    }
  ];

  errorCases.forEach(testCase => {
    it(`should handle HTTP ${testCase.status}: ${testCase.expected}`, () => {
      // Arrange
      const values = {
        employeeIdentifier: 'EMP001',
        rating: MoodRating.PrettyGood,
        comment: 'Keep my comment.'
      };

      component.form.setValue(values);

      moodService.create.and.returnValue(throwError(() =>
        new HttpErrorResponse({
          status: testCase.status,
          error: testCase.body
        })
      ));

      // Act
      component.submit();
      fixture.detectChanges();

      // Assert
      expect(component.errorMessage).toBe(testCase.expected);
      expect(component.successMessage).toBe('');
      expect(component.form.getRawValue()).toEqual(values);
      expect(component.form.enabled).toBeTrue();
      expect(component.isSubmitting).toBeFalse();

      const message = fixture.nativeElement.querySelector('[role="alert"]');
      expect(message?.textContent).toContain(testCase.expected);
    });
  });
});