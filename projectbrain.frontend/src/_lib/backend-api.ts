import { getAccessToken } from './auth';

const API_URL = process.env.API_SERVER_URL || 'https://localhost:7585';

export interface ApiOptions {
    scopes?: string[];
    method?: string;
    contentType?: string;
    body?: unknown;
    cache?: RequestCache;
    revalidate?: number | false;
    isFormData?: boolean;
}

export class BackendApiError extends Error {
    constructor(
        public status: number,
        public message: string,
        public details?: unknown
    ) {
        super(message);
        this.name = 'BackendApiError';
    }
}

export async function callBackendApi(
    endpoint: string,
    options: ApiOptions = {}
): Promise<Response> {
    const {
        scopes = [],
        method = 'GET',
        contentType = 'application/json',
        body,
        cache = 'no-store',
        revalidate,
        isFormData,
    } = options;

    try {
        // Get access token with required scopes
        const accessToken = await getAccessToken({
            scope: scopes.length > 0 ? scopes.join(' ') : undefined,
        });

        // Prepare headers
        const headers: HeadersInit = {
            Authorization: `Bearer ${accessToken}`,
        };
        // Only add content-type when isFormData is not true
        if (!isFormData) {
            headers['Content-Type'] = 'application/json';
        }

        // Prepare request options
        const fetchOptions: RequestInit = {
            method,
            headers: headers,
            cache,
            ...(revalidate !== undefined && { next: { revalidate } }),
        };

        if (
            body &&
            !isFormData &&
            (method === 'POST' || method === 'PUT' || method === 'PATCH')
        ) {
            fetchOptions.body = body
                ? contentType === 'application/json'
                    ? JSON.stringify(body)
                    : (body as BodyInit)
                : undefined;
        }

        // Make request to backend
        const response = await fetch(`${API_URL}${endpoint}`, fetchOptions);

        // Handle non-2xx responses
        if (!response.ok) {
            console.error(
                'Backend API response not ok:',
                `${API_URL}${endpoint}`,
                response.status,
                response.statusText
            );
            const errorData = await response.json().catch(() => ({}));
            throw new BackendApiError(
                response.status,
                errorData.message ||
                    `Backend request failed: ${response.statusText}`,
                errorData
            );
        }

        return response;

        // // Handle no-content responses
        // if (response.status === 204) {
        //     return {} as T;
        // }

        // return await response.json();
    } catch (error: unknown) {
        // Handle insufficient scope errors
        if (
            typeof error === 'object' &&
            error !== null &&
            'code' in error &&
            (error as { code?: string }).code === 'ERR_INSUFFICIENT_SCOPE'
        ) {
            throw new BackendApiError(
                403,
                'You do not have the required permissions',
                { requiredScopes: scopes }
            );
        }

        // Re-throw BackendApiError
        if (error instanceof BackendApiError) {
            throw error;
        }

        // Handle other errors
        console.error('Backend API error:', error);
        throw new BackendApiError(500, 'Internal server error', error);
    }
}
