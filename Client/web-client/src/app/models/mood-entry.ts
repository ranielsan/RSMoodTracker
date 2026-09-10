export enum MoodRating {
    NotGoodAtAll = 1,
    ABitMeh = 2,
    PrettyGood = 3,
    FeelingGreat = 4
}

export interface CreateMoodEntryRequest {
    employeeIdentifier: string;
    rating: MoodRating;
    comment?: string;
}

export interface CreateMoodEntryResponse {
    entryId: number;
    message: string;
}

export interface MoodEntryResponse {
    id: number;
    employeeIdentifier: string;
    rating: MoodRating;
    comment: string | null;
    createdAtUtc: string;
}

export interface MoodDashboardQuery {
    from?: string;
    to?: string;
    rating?: MoodRating;
    page: number;
    pageSize: number;
}

export interface MoodStatistic {
    rating: MoodRating;
    count: number;
    percentage: number;
}

export interface MoodDashboardResponse {
    entries: MoodEntryResponse[];
    totalCount: number;
    page: number;
    pageSize: number;
    statistics: MoodStatistic[];
}
