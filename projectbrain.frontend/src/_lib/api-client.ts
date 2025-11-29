export class ApiClientError extends Error {
    constructor(
        public status: number,
        public message: string,
        public details?: unknown
    ) {
        super(message);
        this.name = 'ApiClientError';
    }
}

export interface ApiClientOptions {
    method?: string;
    body?: unknown;
    headers?: Record<string, string>;
}

export async function apiClient<T>(
    endpoint: string,
    options: ApiClientOptions = {}
): Promise<T> {
    const { method = 'GET', body, headers = {} } = options;

    const isFormData = body instanceof FormData;
    
    const fetchOptions: RequestInit = {
        method,
        headers: {
            // Don't set Content-Type for FormData - let browser set it with boundary
            ...(isFormData ? {} : { 'Content-Type': 'application/json' }),
            ...headers,
        },
    };

    if (body && (method === 'POST' || method === 'PUT' || method === 'PATCH')) {
        fetchOptions.body = isFormData ? body : JSON.stringify(body);
    }

    const response = await fetch(endpoint, fetchOptions);

    if (!response.ok) {
        // Handle 401 Unauthorized - session expired
        if (response.status === 401 || response.headers.get('X-Session-Expired') === 'true') {
            const currentPath = typeof window !== 'undefined' ? window.location.pathname : '/app';
            if (typeof window !== 'undefined') {
                window.location.href = `/auth/login?returnTo=${encodeURIComponent(currentPath)}`;
                // Return a promise that never resolves to prevent further execution
                return new Promise(() => {}) as Promise<T>;
            }
        }
        
        const errorData = await response.json().catch(() => ({}));
        throw new ApiClientError(
            response.status,
            errorData.error || 'Request failed',
            errorData.details
        );
    }

    if (response.status === 204) {
        return {} as T;
    }

    return await response.json();
}

// Convenience methods
export const api = {
    get: (endpoint: string) => apiClient(endpoint, { method: 'GET' }),

    post: (endpoint: string, body: unknown) =>
        apiClient(endpoint, { method: 'POST', body }),

    put: (endpoint: string, body: unknown) =>
        apiClient(endpoint, { method: 'PUT', body }),

    patch: (endpoint: string, body: unknown) =>
        apiClient(endpoint, { method: 'PATCH', body }),

    delete: (endpoint: string) => apiClient(endpoint, { method: 'DELETE' }),
};
