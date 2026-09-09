import { Component } from '@angular/core';
import {
  FormControl,
  FormGroup,
  Validators
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';

import { MoodRating } from '../../models/mood-entry';
import { MoodService } from '../../services/mood.service';

@Component({
  selector: 'app-mood-form',
  templateUrl: './mood-form.component.html',
  styleUrls: ['./mood-form.component.scss']
})
export class MoodFormComponent {
  readonly moodOptions = [
    { value: MoodRating.NotGoodAtAll, label: 'Not good at all' },
    { value: MoodRating.ABitMeh, label: 'A bit “meh”' },
    { value: MoodRating.PrettyGood, label: 'Pretty good' },
    { value: MoodRating.FeelingGreat, label: 'Feeling great' }
  ];

  readonly form = new FormGroup({
    employeeIdentifier: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.pattern(/\S/),
        Validators.maxLength(50)
      ]
    }),
    rating: new FormControl<MoodRating | null>(
      null,
      Validators.required
    ),
    comment: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(1000)]
    })
  });

  isSubmitting = false;
  successMessage = '';
  errorMessage = '';

  constructor(private readonly moodService: MoodService) { }

  submit(): void {
    if (this.isSubmitting) {
      return;
    }

    this.successMessage = '';
    this.errorMessage = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();

    if (values.rating === null) {
      return;
    }

    this.isSubmitting = true;

    this.form.disable();

    this.moodService.create({
      employeeIdentifier: values.employeeIdentifier.trim(),
      rating: values.rating,
      comment: values.comment.trim() || undefined
    })
      .pipe(finalize(() => {
        this.isSubmitting = false;
        this.form.enable();
      }))
      .subscribe({
        next: response => {
          this.successMessage = response.message;
          this.form.reset();
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 0) {
            this.errorMessage =
              'Unable to reach the server. Please try again.';
          } else if (error.status >= 500) {
            this.errorMessage =
              'Something went wrong. Please try again later.';
          } else {
            this.errorMessage =
              error.error?.detail ??
              'Please check your details and try again.';
          }
        }
      });
  }
}