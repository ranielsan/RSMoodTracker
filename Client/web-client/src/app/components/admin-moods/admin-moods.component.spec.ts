import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MoodDashboardResponse } from '../../models/mood-entry';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { provideRouter, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { of, Subject, throwError } from 'rxjs';

import { AdminMoodsComponent } from './admin-moods.component';
import { MoodService } from '../../services/mood.service';
import { AdminAuthService } from '../../services/admin-auth.service';
import {
  MoodEntryResponse,
  MoodRating
} from '../../models/mood-entry';

function dashboard(entries: MoodEntryResponse[]): MoodDashboardResponse {
  return { entries, totalCount: entries.length, page: 1, pageSize: 10, statistics: [] };
}

describe('AdminMoodsComponent', () => {
  let component: AdminMoodsComponent;
  let fixture: ComponentFixture<AdminMoodsComponent>;
  let moodService: jasmine.SpyObj<MoodService>;
  let authService: jasmine.SpyObj<AdminAuthService>;
  let navigate: jasmine.Spy;

  function sampleEntry(): MoodEntryResponse {
    return {
      id: 1,
      employeeIdentifier: 'EMP001',
      rating: MoodRating.PrettyGood,
      comment: 'Good morning.',
      createdAtUtc: '2026-09-09T10:00:00'
    };
  }

  beforeEach(async () => {
    moodService = jasmine.createSpyObj<MoodService>(
      'MoodService',
      ['getDashboard']
    );

    authService = jasmine.createSpyObj<AdminAuthService>(
      'AdminAuthService',
      ['logout']
    );

    moodService.getDashboard.and.returnValue(of(dashboard([])));
    authService.logout.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      declarations: [AdminMoodsComponent],
      imports: [MatDatepickerModule, MatNativeDateModule,
        ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatPaginatorModule, MatProgressBarModule,
        NoopAnimationsModule,
        RouterLink,
        MatCardModule,
        MatTableModule,
        MatButtonModule
      ],
      providers: [
        provideRouter([]),
        { provide: MoodService, useValue: moodService },
        { provide: AdminAuthService, useValue: authService }
      ]
    }).compileComponents();

    navigate = spyOn(
      TestBed.inject(Router),
      'navigateByUrl'
    ).and.resolveTo(true);

    fixture = TestBed.createComponent(AdminMoodsComponent);
    component = fixture.componentInstance;

    // Each test triggers initial rendering after arranging its response.
  });

  it('should load and display entries on initialization', () => {
    // Arrange
    moodService.getDashboard.and.returnValue(of(dashboard([sampleEntry()])));

    // Act
    fixture.detectChanges();

    // Assert
    expect(moodService.getDashboard).toHaveBeenCalledTimes(1);
    expect(component.isLoading).toBeFalse();
    expect(component.entries[0].createdAtUtc)
      .toBe('2026-09-09T10:00:00Z');

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('EMP001');
    expect(element.textContent).toContain('Pretty good');
    expect(element.textContent).toContain('Good morning.');
  });

  it('should display an empty state', () => {
    // Arrange
    moodService.getDashboard.and.returnValue(of(dashboard([])));

    // Act
    fixture.detectChanges();

    // Assert
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('No mood entries match your filters.');
    expect(element.querySelector('table')).toBeNull();
  });

  it('should preserve the order returned by the API', () => {
    // Arrange
    const newer = {
      ...sampleEntry(),
      id: 2,
      createdAtUtc: '2026-09-10T10:00:00Z'
    };
    const older = sampleEntry();
    moodService.getDashboard.and.returnValue(of(dashboard([newer, older])));

    // Act
    fixture.detectChanges();

    // Assert
    expect(component.entries.map(entry => entry.id)).toEqual([2, 1]);
  });

  it('should preserve timestamps that already include a timezone', () => {
    // Arrange
    moodService.getDashboard.and.returnValue(of(dashboard([
      { ...sampleEntry(), createdAtUtc: '2026-09-09T10:00:00Z' },
      {
        ...sampleEntry(),
        id: 2,
        createdAtUtc: '2026-09-09T18:00:00+08:00'
      }
    ])));

    // Act
    fixture.detectChanges();

    // Assert
    expect(component.entries[0].createdAtUtc)
      .toBe('2026-09-09T10:00:00Z');
    expect(component.entries[1].createdAtUtc)
      .toBe('2026-09-09T18:00:00+08:00');
  });

  it('should show a placeholder for a missing comment', () => {
    // Arrange
    moodService.getDashboard.and.returnValue(of(dashboard([
      { ...sampleEntry(), comment: null }
    ])));

    // Act
    fixture.detectChanges();

    // Assert
    const cell = fixture.nativeElement.querySelector('td.comment');
    expect(cell?.textContent.trim()).toBe('—');
  });

  it('should refresh the displayed entries', () => {
    // Arrange
    fixture.detectChanges();
    moodService.getDashboard.and.returnValue(of(dashboard([sampleEntry()])));

    // Act
    component.loadEntries();
    fixture.detectChanges();

    // Assert
    expect(moodService.getDashboard).toHaveBeenCalledTimes(2);
    expect(component.entries.length).toBe(1);
  });

  it('should prevent refresh and logout while loading', () => {
    // Arrange
    const pending = new Subject<MoodDashboardResponse>();
    moodService.getDashboard.and.returnValue(pending);

    // Act
    fixture.detectChanges();
    component.loadEntries();
    component.logout();

    // Assert
    expect(component.isLoading).toBeTrue();
    expect(moodService.getDashboard).toHaveBeenCalledTimes(1);
    expect(authService.logout).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent)
      .toContain('Loading mood entries…');

    // Act: finish loading
    pending.next(dashboard([]));
    pending.complete();
    fixture.detectChanges();

    // Assert
    expect(component.isLoading).toBeFalse();
  });

  [401, 403].forEach(status => {
    it(`should redirect to login when loading returns ${status}`, () => {
      // Arrange
      moodService.getDashboard.and.returnValue(throwError(() =>
        new HttpErrorResponse({ status })
      ));
      component.entries = [sampleEntry()];

      // Act
      fixture.detectChanges();

      // Assert
      expect(navigate).toHaveBeenCalledOnceWith('/admin/login');
      expect(component.entries).toEqual([]);
      expect(component.isLoading).toBeFalse();
    });
  });

  [0, 500].forEach(status => {
    it(`should show a loading error for HTTP ${status}`, () => {
      // Arrange
      moodService.getDashboard.and.returnValue(throwError(() =>
        new HttpErrorResponse({ status })
      ));

      // Act
      fixture.detectChanges();

      // Assert
      expect(component.errorMessage)
        .toBe('Unable to load mood entries. Please try again.');
      expect(component.isLoading).toBeFalse();
      expect(navigate).not.toHaveBeenCalled();
      expect(
        fixture.nativeElement.querySelector('[role="alert"]')?.textContent
      ).toContain('Unable to load mood entries.');
    });
  });

  it('should clear entries and navigate after logout', () => {
    // Arrange
    moodService.getDashboard.and.returnValue(of(dashboard([sampleEntry()])));
    fixture.detectChanges();

    // Act
    component.logout();

    // Assert
    expect(authService.logout).toHaveBeenCalledTimes(1);
    expect(component.entries).toEqual([]);
    expect(component.isLoggingOut).toBeFalse();
    expect(navigate).toHaveBeenCalledOnceWith('/admin/login');
  });

  it('should prevent repeated logout and refresh while signing out', () => {
    // Arrange
    fixture.detectChanges();
    const pending = new Subject<void>();
    authService.logout.and.returnValue(pending);

    // Act
    component.logout();
    component.logout();
    component.loadEntries();
    fixture.detectChanges();

    // Assert
    expect(component.isLoggingOut).toBeTrue();
    expect(authService.logout).toHaveBeenCalledTimes(1);
    expect(moodService.getDashboard).toHaveBeenCalledTimes(1);
    expect(fixture.nativeElement.textContent).toContain('Signing out…');

    // Act: finish logout
    pending.next();
    pending.complete();

    // Assert
    expect(component.isLoggingOut).toBeFalse();
    expect(navigate).toHaveBeenCalledOnceWith('/admin/login');
  });

  it('should redirect when the session has already expired during logout', () => {
    // Arrange
    fixture.detectChanges();
    component.entries = [sampleEntry()];
    authService.logout.and.returnValue(throwError(() =>
      new HttpErrorResponse({ status: 401 })
    ));

    // Act
    component.logout();

    // Assert
    expect(component.entries).toEqual([]);
    expect(component.isLoggingOut).toBeFalse();
    expect(navigate).toHaveBeenCalledOnceWith('/admin/login');
  });

  [0, 403, 500].forEach(status => {
    it(`should show a logout error for HTTP ${status}`, () => {
      // Arrange
      moodService.getDashboard.and.returnValue(of(dashboard([sampleEntry()])));
      fixture.detectChanges();
      authService.logout.and.returnValue(throwError(() =>
        new HttpErrorResponse({ status })
      ));

      // Act
      component.logout();
      fixture.detectChanges();

      // Assert
      expect(component.errorMessage)
        .toBe('Unable to sign out. Please try again.');
      expect(component.entries.length).toBe(1);
      expect(component.isLoggingOut).toBeFalse();
      expect(navigate).not.toHaveBeenCalled();
      expect(
        fixture.nativeElement.querySelector('[role="alert"]')?.textContent
      ).toContain('Unable to sign out.');
    });
  });

  it('should apply filters and reset to page one', () => {
    // Arrange
    fixture.detectChanges();
    component.query.page = 3;
    component.filters.setValue({ from: new Date(2026, 8, 1), to: new Date(2026, 8, 10), rating: MoodRating.PrettyGood });
    // Act
    component.applyFilters();
    // Assert
    expect(moodService.getDashboard).toHaveBeenCalledWith({ from: '2026-09-01', to: '2026-09-10', rating: MoodRating.PrettyGood, page: 1, pageSize: 10 });
  });

  it('should reject reversed dates without requesting data', () => {
    // Arrange
    fixture.detectChanges();
    component.filters.patchValue({ from: new Date(2026, 8, 11), to: new Date(2026, 8, 10) });
    // Act
    component.applyFilters();
    // Assert
    expect(moodService.getDashboard).toHaveBeenCalledTimes(1);
    expect(component.filterError).toContain('From on or before To');
  });

  it('should clear filters and preserve the chosen page size', () => {
    // Arrange
    fixture.detectChanges();
    component.query.pageSize = 25;
    component.filters.patchValue({ from: new Date(2026, 8, 1), rating: MoodRating.FeelingGreat });
    // Act
    component.clearFilters();
    // Assert
    expect(moodService.getDashboard).toHaveBeenCalledWith({ from: undefined, to: undefined, rating: undefined, page: 1, pageSize: 25 });
  });

  it('should request another page with the applied filters', () => {
    // Arrange
    fixture.detectChanges();
    component.query.from = '2026-09-01';
    // Act
    component.changePage({ pageIndex: 1, pageSize: 10, length: 30 });
    // Assert
    expect(moodService.getDashboard).toHaveBeenCalledWith({ from: '2026-09-01', to: component.query.to, page: 2, pageSize: 10 });
  });

  it('should display backend totals and percentages independently of page entries', () => {
    // Arrange
    moodService.getDashboard.and.returnValue(of({ entries: [sampleEntry()], totalCount: 40, page: 1, pageSize: 10,
      statistics: [{ rating: MoodRating.PrettyGood, count: 12, percentage: 30 }] }));
    // Act
    fixture.detectChanges();
    // Assert
    expect(component.totalCount).toBe(40);
    expect(fixture.nativeElement.textContent).toContain('30%');
    expect(fixture.nativeElement.textContent).toContain('12 submissions');
    expect(fixture.nativeElement.textContent).toContain('40 matching submissions');
  });
  it('should request today in UTC on initial load', () => {
    // Arrange
    const now = new Date();
    const today = now.toISOString().slice(0, 10);
    // Act
    fixture.detectChanges();
    // Assert
    expect(moodService.getDashboard).toHaveBeenCalledWith({ from: today, to: today, page: 1, pageSize: 10 });
    expect(component.filters.controls.from.value?.getDate()).toBe(now.getUTCDate());
  });
});