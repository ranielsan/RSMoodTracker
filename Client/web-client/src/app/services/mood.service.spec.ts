import { TestBed } from '@angular/core/testing';
import {
    HttpErrorResponse,
    provideHttpClient
} from '@angular/common/http';
import {
    HttpTestingController,
    provideHttpClientTesting
} from '@angular/common/http/testing';

import { MoodService } from './mood.service';
import { MoodRating } from '../models/mood-entry';
import { environment } from '../../environments/environment';

describe('MoodService', () => {
    let service: MoodService;
    let httpTesting: HttpTestingController;

    const endpoint =
        `${environment.apiUrl.replace(/\/$/, '')}/api/moods`;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                MoodService,
                provideHttpClient(),
                provideHttpClientTesting()
            ]
        });

        service = TestBed.inject(MoodService);
        httpTesting = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpTesting.verify();
    });

    it('should POST the submission and return the response', () => {
        // Arrange
        const request = {
            employeeIdentifier: 'EMP001',
            rating: MoodRating.PrettyGood,
            comment: 'Good morning.'
        };

        const response = {
            entryId: 1,
            message: 'Your mood has been recorded.'
        };

        const next = jasmine.createSpy('next');

        // Act
        service.create(request).subscribe({
            next,
            error: fail
        });

        // Assert
        const pending = httpTesting.expectOne(endpoint);
        expect(pending.request.method).toBe('POST');
        expect(pending.request.body).toEqual(request);

        // Act: simulate the API response
        pending.flush(response, {
            status: 201,
            statusText: 'Created'
        });

        // Assert
        expect(next).toHaveBeenCalledOnceWith(response);
    });

    it('should pass a duplicate-submission error to the caller', () => {
        // Arrange
        const request = {
            employeeIdentifier: 'EMP001',
            rating: MoodRating.PrettyGood
        };

        const problem = {
            status: 409,
            title: 'Mood already submitted',
            detail: 'You have already recorded your mood today.'
        };

        const onError = jasmine.createSpy('onError');

        // Act
        service.create(request).subscribe({
            next: () => fail('Expected an HTTP error.'),
            error: onError
        });

        const pending = httpTesting.expectOne(endpoint);
        pending.flush(problem, {
            status: 409,
            statusText: 'Conflict'
        });

        // Assert
        expect(onError).toHaveBeenCalledTimes(1);

        const error = onError.calls.mostRecent()
            .args[0] as HttpErrorResponse;

        expect(error.status).toBe(409);
        expect(error.error).toEqual(problem);
    });

    it('should send dashboard filters and credentials and return the summary', () => {
        // Arrange
        const response = { entries: [], totalCount: 0, page: 2, pageSize: 25, statistics: [] };
        const next = jasmine.createSpy('next');
        // Act
        service.getDashboard({ from: '2026-09-01', to: '2026-09-10', rating: MoodRating.PrettyGood, page: 2, pageSize: 25 }).subscribe(next);
        // Assert
        const pending = httpTesting.expectOne(req => req.url.endsWith('/api/admin/moods/dashboard'));
        expect(pending.request.withCredentials).toBeTrue();
        expect(pending.request.params.get('from')).toBe('2026-09-01');
        expect(pending.request.params.get('to')).toBe('2026-09-10');
        expect(pending.request.params.get('rating')).toBe('3');
        expect(pending.request.params.get('page')).toBe('2');
        expect(pending.request.params.get('pageSize')).toBe('25');
        pending.flush(response);
        expect(next).toHaveBeenCalledOnceWith(response);
    });

    it('should omit unset filters and propagate dashboard errors', () => {
        // Arrange
        const error = jasmine.createSpy('error');
        // Act
        service.getDashboard({ page: 1, pageSize: 10 }).subscribe({ error });
        // Assert
        const pending = httpTesting.expectOne(req => req.url.endsWith('/api/admin/moods/dashboard'));
        expect(pending.request.params.keys()).toEqual(['page', 'pageSize']);
        pending.flush({}, { status: 401, statusText: 'Unauthorized' });
        expect(error.calls.mostRecent().args[0].status).toBe(401);
    });
});