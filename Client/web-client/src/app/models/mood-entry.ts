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