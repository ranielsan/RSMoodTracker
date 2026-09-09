import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';

import { AdminAuthService } from './admin-auth.service';
import { environment } from '../../environments/environment';

describe('AdminAuthService', () => {
  let service: AdminAuthService;
  let httpTesting: HttpTestingController;

  const endpoint =
    `${environment.apiUrl.replace(/\/$/, '')}/api/admin/auth`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdminAuthService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AdminAuthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should fetch a CSRF token before logging in with cookies', () => {
    // Arrange
    const credentials = {
      username: 'admin',
      password: 'test-password'
    };

    const response = {
      message: 'Signed in successfully.'
    };

    const next = jasmine.createSpy('next');

    // Act
    service.login(credentials).subscribe({ next, error: fail });

    // Assert: login waits for the token
    const csrf = httpTesting.expectOne(`${endpoint}/csrf`);
    expect(csrf.request.method).toBe('GET');
    expect(csrf.request.withCredentials).toBeTrue();
    httpTesting.expectNone(`${endpoint}/login`);

    // Act: return the token
    csrf.flush({ token: 'login-token' });

    // Assert: login includes the token and cookies
    const login = httpTesting.expectOne(`${endpoint}/login`);
    expect(login.request.method).toBe('POST');
    expect(login.request.body).toEqual(credentials);
    expect(login.request.withCredentials).toBeTrue();
    expect(login.request.headers.get('X-CSRF-TOKEN'))
      .toBe('login-token');

    // Act
    login.flush(response);

    // Assert
    expect(next).toHaveBeenCalledOnceWith(response);
  });

  it('should fetch a fresh CSRF token before logging out', () => {
    // Arrange
    const complete = jasmine.createSpy('complete');

    // Act
    service.logout().subscribe({ complete, error: fail });

    // Assert
    const csrf = httpTesting.expectOne(`${endpoint}/csrf`);
    expect(csrf.request.withCredentials).toBeTrue();
    httpTesting.expectNone(`${endpoint}/logout`);

    // Act
    csrf.flush({ token: 'logout-token' });

    // Assert
    const logout = httpTesting.expectOne(`${endpoint}/logout`);
    expect(logout.request.method).toBe('POST');
    expect(logout.request.withCredentials).toBeTrue();
    expect(logout.request.headers.get('X-CSRF-TOKEN'))
      .toBe('logout-token');

    // Act
    logout.flush(null, {
      status: 204,
      statusText: 'No Content'
    });

    // Assert
    expect(complete).toHaveBeenCalledTimes(1);
  });

  it('should check the session with cookies', () => {
    // Arrange
    const next = jasmine.createSpy('next');
    const response = { username: 'admin' };

    // Act
    service.getSession().subscribe({ next, error: fail });

    // Assert
    const request = httpTesting.expectOne(`${endpoint}/session`);
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBeTrue();

    // Act
    request.flush(response);

    // Assert
    expect(next).toHaveBeenCalledOnceWith(response);
  });

  it('should not send login when fetching the CSRF token fails', () => {
    // Arrange
    const onError = jasmine.createSpy('onError');

    // Act
    service.login({
      username: 'admin',
      password: 'test-password'
    }).subscribe({
      next: () => fail('Expected an error.'),
      error: onError
    });

    httpTesting.expectOne(`${endpoint}/csrf`).flush(null, {
      status: 500,
      statusText: 'Internal Server Error'
    });

    // Assert
    httpTesting.expectNone(`${endpoint}/login`);
    expect(onError).toHaveBeenCalledTimes(1);
    expect(onError.calls.mostRecent().args[0].status).toBe(500);
  });
});