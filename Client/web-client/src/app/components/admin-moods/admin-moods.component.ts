import { Component, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { MoodEntryResponse, MoodRating } from '../../models/mood-entry';
import { MoodService } from '../../services/mood.service';
import { AdminAuthService } from '../../services/admin-auth.service';

@Component({
  selector: 'app-admin-moods',
  templateUrl: './admin-moods.component.html',
  styleUrls: ['./admin-moods.component.scss']
})
export class AdminMoodsComponent implements OnInit {
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

    this.moodService.getAll()
      .pipe(finalize(() => {
        this.isLoading = false;
      }))
      .subscribe({
        next: entries => {
          this.entries = entries.map(entry => ({
            ...entry,
            createdAtUtc: this.normalizeUtc(entry.createdAtUtc)
          }));
        },
        error: (error: HttpErrorResponse) => {
          this.entries = [];

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