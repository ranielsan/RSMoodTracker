import {
  ComponentFixture,
  TestBed,
  fakeAsync,
  flush,
  flushMicrotasks
} from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { provideRouter, Router } from '@angular/router';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { of, Subject, throwError } from 'rxjs';

import { AdminLoginComponent } from './admin-login.component';
import { AdminAuthService } from '../../services/admin-auth.service';
import { AdminLoginResponse } from '../../models/admin-auth';

describe('AdminLoginComponent', () => {
  let component: AdminLoginComponent;
  let fixture: ComponentFixture<AdminLoginComponent>;
  let authService: jasmine.SpyObj<AdminAuthService>;
  let navigate: jasmine.Spy;

  beforeEach(async () => {
    authService = jasmine.createSpyObj<AdminAuthService>(
      'AdminAuthService',
      ['login']
    );

    await TestBed.configureTestingModule({
      declarations: [AdminLoginComponent],
      imports: [
        ReactiveFormsModule,
        NoopAnimationsModule,
        RouterLink,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule
      ],
      providers: [
        provideRouter([]),
        { provide: AdminAuthService, useValue: authService }
      ]
    }).compileComponents();

    navigate = spyOn(
      TestBed.inject(Router),
      'navigateByUrl'
    ).and.resolveTo(true);

    fixture = TestBed.createComponent(AdminLoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should reject an empty form without calling login', () => {
    // Arrange
    component.form.reset();

    // Act
    component.submit();
    fixture.detectChanges();

    // Assert
    expect(authService.login).not.toHaveBeenCalled();
    expect(component.form.controls.username.touched).toBeTrue();
    expect(component.form.controls.password.touched).toBeTrue();
    expect(component.isSubmitting).toBeFalse();
    expect(navigate).not.toHaveBeenCalled();
  });

  it('should reject a whitespace-only username', () => {
    // Arrange
    component.form.setValue({
      username: '   ',
      password: 'test-password'
    });

    // Act
    component.submit();

    // Assert
    expect(authService.login).not.toHaveBeenCalled();
    expect(component.form.controls.username.invalid).toBeTrue();
  });

  it('should sign in, clear the password, and navigate', fakeAsync(() => {
    // Arrange
    component.form.setValue({
      username: '  admin  ',
      password: ' password-with-spaces '
    });

    authService.login.and.returnValue(of({
      message: 'Signed in successfully.'
    }));

    // Act
    component.submit();
    flushMicrotasks();

    // Assert
    expect(authService.login).toHaveBeenCalledOnceWith({
      username: 'admin',
      password: ' password-with-spaces '
    });
    expect(component.form.controls.password.value).toBe('');
    expect(navigate).toHaveBeenCalledOnceWith('/admin/moods');
    expect(component.isSubmitting).toBeFalse();
    expect(component.form.enabled).toBeTrue();
    expect(component.errorMessage).toBe('');
  }));

  it('should disable the form and prevent repeated login requests', fakeAsync(() => {
    // Arrange
    component.form.setValue({
      username: 'admin',
      password: 'test-password'
    });

    const response = new Subject<AdminLoginResponse>();
    authService.login.and.returnValue(response);

    // Act
    component.submit();
    component.submit();
    fixture.detectChanges();

    // Assert
    expect(authService.login).toHaveBeenCalledTimes(1);
    expect(component.form.disabled).toBeTrue();
    expect(component.isSubmitting).toBeTrue();

    const button = fixture.nativeElement.querySelector(
      'button[type="submit"]'
    ) as HTMLButtonElement;

    expect(button.disabled).toBeTrue();

    // Act: finish the request
    response.next({ message: 'Signed in successfully.' });
    response.complete();
    flushMicrotasks();
    fixture.detectChanges();
    flush();

    // Assert
    expect(component.form.enabled).toBeTrue();
    expect(component.isSubmitting).toBeFalse();
    expect(button.disabled).toBeFalse();
  }));

  const errorCases = [
    {
      status: 401,
      body: { detail: 'Invalid username or password.' },
      message: 'Invalid username or password.'
    },
    {
      status: 401,
      body: {
        detail: 'Unable to sign in. Please try again later.'
      },
      message: 'Unable to sign in. Please try again later.'
    },
    {
      status: 403,
      body: {
        detail: 'This account cannot access the admin area.'
      },
      message: 'This account cannot access the admin area.'
    },
    {
      status: 0,
      body: null,
      message: 'Unable to reach the server. Please try again.'
    },
    {
      status: 500,
      body: { detail: 'Internal server details' },
      message: 'Something went wrong. Please try again later.'
    },
    {
      status: 400,
      body: {},
      message: 'Unable to sign in. Please try again.'
    }
  ];

  errorCases.forEach(testCase => {
    it(`should show login error: ${testCase.message}`, () => {
      // Arrange
      component.form.setValue({
        username: 'admin',
        password: 'test-password'
      });

      authService.login.and.returnValue(throwError(() =>
        new HttpErrorResponse({
          status: testCase.status,
          error: testCase.body
        })
      ));

      // Act
      component.submit();
      fixture.detectChanges();

      // Assert
      expect(component.errorMessage).toBe(testCase.message);
      expect(component.form.controls.username.value).toBe('admin');
      expect(component.form.controls.password.value).toBe('');
      expect(component.form.enabled).toBeTrue();
      expect(component.isSubmitting).toBeFalse();
      expect(navigate).not.toHaveBeenCalled();

      const alert = fixture.nativeElement.querySelector('[role="alert"]');
      expect(alert?.textContent).toContain(testCase.message);
    });
  });

  it('should show feedback if navigation is cancelled', fakeAsync(() => {
    // Arrange
    component.form.setValue({
      username: 'admin',
      password: 'test-password'
    });
    authService.login.and.returnValue(of({
      message: 'Signed in successfully.'
    }));
    navigate.and.resolveTo(false);

    // Act
    component.submit();
    flushMicrotasks();

    // Assert
    expect(component.errorMessage).toBe(
      'Unable to open the admin page. Please try again.'
    );
  }));

  it('should show feedback if the session check prevents navigation', fakeAsync(() => {
    // Arrange
    component.form.setValue({
      username: 'admin',
      password: 'test-password'
    });
    authService.login.and.returnValue(of({
      message: 'Signed in successfully.'
    }));
    navigate.and.rejectWith(new Error('Session request failed'));

    // Act
    component.submit();
    flushMicrotasks();

    // Assert
    expect(component.errorMessage).toBe(
      'Unable to verify your session. Check your connection and try again.'
    );
  }));
});