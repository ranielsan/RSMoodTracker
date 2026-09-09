import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, switchMap } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  AdminLoginRequest,
  AdminLoginResponse,
  AdminSession,
  CsrfTokenResponse
} from '../models/admin-auth';

@Injectable({
  providedIn: 'root'
})
export class AdminAuthService {
  private readonly endpoint =
    `${environment.apiUrl.replace(/\/$/, '')}/api/admin/auth`;

  constructor(private readonly http: HttpClient) {}

  login(request: AdminLoginRequest): Observable<AdminLoginResponse> {
    return this.getCsrfToken().pipe(
      switchMap(response =>
        this.http.post<AdminLoginResponse>(
          `${this.endpoint}/login`,
          request,
          {
            withCredentials: true,
            headers: {
              'X-CSRF-TOKEN': response.token
            }
          }
        )
      )
    );
  }

  logout(): Observable<void> {
    return this.getCsrfToken().pipe(
      switchMap(response =>
        this.http.post<void>(
          `${this.endpoint}/logout`,
          {},
          {
            withCredentials: true,
            headers: {
              'X-CSRF-TOKEN': response.token
            }
          }
        )
      )
    );
  }

  getSession(): Observable<AdminSession> {
    return this.http.get<AdminSession>(
      `${this.endpoint}/session`,
      { withCredentials: true }
    );
  }

  private getCsrfToken(): Observable<CsrfTokenResponse> {
    return this.http.get<CsrfTokenResponse>(
      `${this.endpoint}/csrf`,
      { withCredentials: true }
    );
  }
}