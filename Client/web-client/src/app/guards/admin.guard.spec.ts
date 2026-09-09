import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import {
    ActivatedRouteSnapshot,
    provideRouter,
    Router,
    RouterStateSnapshot,
    UrlTree
} from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';

import { adminGuard } from './admin.guard';
import { AdminAuthService } from '../services/admin-auth.service';

describe('adminGuard', () => {
    let authService: jasmine.SpyObj<AdminAuthService>;
    let router: Router;

    beforeEach(() => {
        authService = jasmine.createSpyObj<AdminAuthService>(
            'AdminAuthService',
            ['getSession']
        );

        TestBed.configureTestingModule({
            providers: [
                provideRouter([]),
                { provide: AdminAuthService, useValue: authService }
            ]
        });

        router = TestBed.inject(Router);
    });

    function runGuard(): Promise<boolean | UrlTree> {
        const result = TestBed.runInInjectionContext(() =>
            adminGuard(
                {} as ActivatedRouteSnapshot,
                { url: '/admin/moods' } as RouterStateSnapshot
            )
        );

        return firstValueFrom(result as Observable<boolean | UrlTree>);
    }

    it('should allow access when the session is valid', async () => {
        // Arrange
        authService.getSession.and.returnValue(of({
            username: 'admin'
        }));

        // Act
        const result = await runGuard();

        // Assert
        expect(result).toBeTrue();
        expect(authService.getSession).toHaveBeenCalledTimes(1);
    });

    [401, 403].forEach(status => {
        it(`should redirect to login for HTTP ${status}`, async () => {
            // Arrange
            authService.getSession.and.returnValue(throwError(() =>
                new HttpErrorResponse({ status })
            ));

            // Act
            const result = await runGuard();

            // Assert
            expect(result instanceof UrlTree).toBeTrue();
            expect(router.serializeUrl(result as UrlTree))
                .toBe('/admin/login');
        });
    });

    [0, 500].forEach(status => {
        it(`should propagate HTTP ${status} instead of treating it as signed out`, async () => {
            // Arrange
            const error = new HttpErrorResponse({ status });
            authService.getSession.and.returnValue(
                throwError(() => error)
            );

            // Act
            const result = runGuard();

            // Assert
            await expectAsync(result).toBeRejectedWith(error);
        });
    });
});