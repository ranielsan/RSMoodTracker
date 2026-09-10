import { Component, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { FormControl, FormGroup } from '@angular/forms';
import { PageEvent } from '@angular/material/paginator';
import { MoodDashboardQuery, MoodStatistic } from '../../models/mood-entry';

import { MoodEntryResponse, MoodRating } from '../../models/mood-entry';
import { MoodService } from '../../services/mood.service';
import { AdminAuthService } from '../../services/admin-auth.service';

@Component({
  selector: 'app-admin-moods',
  templateUrl: './admin-moods.component.html',
  styleUrls: ['./admin-moods.component.scss']
})
export class AdminMoodsComponent implements OnInit {
  private readonly now = new Date();
  private readonly today = new Date(this.now.getUTCFullYear(), this.now.getUTCMonth(), this.now.getUTCDate());

  private dateParameter(value: Date | null): string | undefined {
    if (!value) return undefined;
    return [value.getFullYear(), String(value.getMonth() + 1).padStart(2, '0'), String(value.getDate()).padStart(2, '0')].join('-');
  }

  readonly filters = new FormGroup({
    from: new FormControl<Date | null>(new Date(this.today)),
    to: new FormControl<Date | null>(new Date(this.today)),
    rating: new FormControl<MoodRating | null>(null)
  });
  readonly ratings = [MoodRating.NotGoodAtAll, MoodRating.ABitMeh, MoodRating.PrettyGood, MoodRating.FeelingGreat];
  query: MoodDashboardQuery = { from: this.dateParameter(this.today), to: this.dateParameter(this.today), page: 1, pageSize: 10 };
  statistics: MoodStatistic[] = [];
  totalCount = 0;
  filterError = '';

  applyFilters(): void {
    if (this.isLoading || this.isLoggingOut) return;
    const values = this.filters.getRawValue();
    if (this.filters.invalid || (values.from && values.to && values.from > values.to)) {
      this.filterError = 'Enter valid dates, with From on or before To.';
      return;
    }
    this.filterError = '';
    this.query = { from: this.dateParameter(values.from), to: this.dateParameter(values.to),
      rating: values.rating ?? undefined, page: 1, pageSize: this.query.pageSize };
    this.loadEntries();
  }

  clearFilters(): void {
    if (this.isLoading || this.isLoggingOut) return;
    this.filters.reset();
    this.applyFilters();
  }

  changePage(event: PageEvent): void {
    if (this.isLoading || this.isLoggingOut) return;
    this.query = { ...this.query, page: event.pageIndex + 1, pageSize: event.pageSize };
    this.loadEntries();
  }
  readonly displayedColumns = [
    'employee',
    'mood',
    'comment',
    'createdAt'
  ];

  readonly moodLabels: Record<MoodRating, string> = {
    [MoodRating.NotGoodAtAll]: 'Not good at all',
    [MoodRating.ABitMeh]: 'A bit “meh”',
    [MoodRating.PrettyGood]: 'Pretty good',
    [MoodRating.FeelingGreat]: 'Feeling great'
  };

  entries: MoodEntryResponse[] = [];
  isLoading = false;
  isLoggingOut = false;
  errorMessage = '';

  constructor(
    private readonly moodService: MoodService,
    private readonly authService: AdminAuthService,
    private readonly router: Router
  ) { }

  ngOnInit(): void {
    this.loadEntries();
  }

  loadEntries(): void {
    if (this.isLoading || this.isLoggingOut) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.moodService.getDashboard(this.query)
      .pipe(finalize(() => {
        this.isLoading = false;
      }))
      .subscribe({
        next: response => {
          this.totalCount = response.totalCount;
          this.statistics = response.statistics;
          this.query = { ...this.query, page: response.page, pageSize: response.pageSize };
          this.entries = response.entries.map(entry => ({
            ...entry,
            createdAtUtc: this.normalizeUtc(entry.createdAtUtc)
          }));
        },
        error: (error: HttpErrorResponse) => {
          this.entries = [];
          this.statistics = [];
          this.totalCount = 0;

          if (error.status === 401 || error.status === 403) {
            void this.router.navigateByUrl('/admin/login');
            return;
          }

          this.errorMessage =
            'Unable to load mood entries. Please try again.';
        }
      });
  }

  logout(): void {
    if (this.isLoggingOut || this.isLoading) {
      return;
    }

    this.isLoggingOut = true;
    this.errorMessage = '';

    this.authService.logout()
      .pipe(finalize(() => {
        this.isLoggingOut = false;
      }))
      .subscribe({
        next: () => {
          this.entries = [];
          void this.router.navigateByUrl('/admin/login');
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 401) {
            this.entries = [];
            void this.router.navigateByUrl('/admin/login');
            return;
          }

          this.errorMessage =
            'Unable to sign out. Please try again.';
        }
      });
  }

  private normalizeUtc(value: string): string {
    return /(?:Z|[+-]\d{2}:\d{2})$/i.test(value)
      ? value
      : `${value}Z`;
  }

  getMoodLabel(rating: MoodRating): string {
    return this.moodLabels[rating] ?? 'Unknown mood';
  }
}
