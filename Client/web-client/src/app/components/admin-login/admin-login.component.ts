import { Component } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { AdminAuthService } from '../../services/admin-auth.service';

@Component({
  selector: 'app-admin-login',
  templateUrl: './admin-login.component.html',
  styleUrls: ['./admin-login.component.scss']
})
export class AdminLoginComponent {
  readonly form = new FormGroup({
    username: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.pattern(/\S/),
        Validators.maxLength(256)
      ]
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    })
  });

  isSubmitting = false;
  errorMessage = '';

  constructor(
    private readonly authService: AdminAuthService,
    private readonly router: Router
  ) { }

  submit(): void {
    if (this.isSubmitting) {
      return;
    }

    this.errorMessage = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();

    this.isSubmitting = true;
    this.form.disable();

    this.authService.login({
      username: values.username.trim(),
      password: values.password
    })
      .pipe(finalize(() => {
        this.isSubmitting = false;
        this.form.enable();
      }))
      .subscribe({
        next: () => {
          this.form.controls.password.reset();

          this.router.navigateByUrl('/admin/moods')
            .then(navigated => {
              if (!navigated) {
                this.errorMessage =
                  'Unable to open the admin page. Please try again.';
              }
            })
            .catch(() => {
              this.errorMessage =
                'Unable to verify your session. Check your connection and try again.';
            });
        },
        error: (error: HttpErrorResponse) => {
          this.form.controls.password.reset();

          if (error.status === 0) {
            this.errorMessage =
              'Unable to reach the server. Please try again.';
          } else if (error.status >= 500) {
            this.errorMessage =
              'Something went wrong. Please try again later.';
          } else {
            this.errorMessage =
              error.error?.detail ??
              'Unable to sign in. Please try again.';
          }
        }
      });
  }
}