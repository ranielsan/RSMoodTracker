import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
    CreateMoodEntryRequest,
    CreateMoodEntryResponse
} from '../models/mood-entry';

@Injectable({
    providedIn: 'root'
})
export class MoodService {
    private readonly endpoint =
        `${environment.apiUrl.replace(/\/$/, '')}/api/moods`;

    constructor(private readonly http: HttpClient) { }

    create(request: CreateMoodEntryRequest): Observable<CreateMoodEntryResponse> {
        return this.http.post<CreateMoodEntryResponse>(
            this.endpoint,
            request
        );
    }
}