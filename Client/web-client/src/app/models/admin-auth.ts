export interface AdminLoginRequest {
    username: string;
    password: string;
}

export interface AdminLoginResponse {
    message: string;
}

export interface AdminSession {
    username: string;
}

export interface CsrfTokenResponse {
    token: string;
}