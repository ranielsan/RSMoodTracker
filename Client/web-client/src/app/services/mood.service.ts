import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MoodDashboardQuery, MoodDashboardResponse } from '../models/mood-entry';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
    CreateMoodEntryRequest,
    CreateMoodEntryResponse,
    MoodEntryResponse
} from '../models/mood-entry';

@Injectable({
    providedIn: 'root'
})
export class MoodService {
    private readonly endpoint =
        `${environment.apiUrl.replace(/\/$/, '')}/api/moods`;

    constructor(private readonly http: HttpClient) { }

    getDashboard(query: MoodDashboardQuery): Observable<MoodDashboardResponse> {
        let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
        if (query.from) params = params.set('from', query.from);
        if (query.to) params = params.set('to', query.to);
        if (query.rating !== undefined) params = params.set('rating', query.rating);
        return this.http.get<MoodDashboardResponse>(
            `${environment.apiUrl.replace(/\/$/, '')}/api/admin/moods/dashboard`,
            { params, withCredentials: true });
    }

    create(request: CreateMoodEntryRequest): Observable<CreateMoodEntryResponse> {
        return this.http.post<CreateMoodEntryResponse>(
            this.endpoint,
            request
        );
    }

    getAll(): Observable<MoodEntryResponse[]> {
        const url =
            `${environment.apiUrl.replace(/\/$/, '')}/api/admin/moods`;

        return this.http.get<MoodEntryResponse[]>(url, {
            withCredentials: true
        });
    }
}
