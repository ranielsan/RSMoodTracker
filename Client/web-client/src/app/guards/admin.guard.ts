import { inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of, throwError } from 'rxjs';

import { AdminAuthService } from '../services/admin-auth.service';

export const adminGuard: CanActivateFn = () => {
    const authService = inject(AdminAuthService);
    const router = inject(Router);

    return authService.getSession().pipe(
        map(() => true),
        catchError((error: HttpErrorResponse) => {
            if (error.status === 401 || error.status === 403) {
                return of(router.createUrlTree(['/admin/login']));
            }

            return throwError(() => error);
        })
    );
};