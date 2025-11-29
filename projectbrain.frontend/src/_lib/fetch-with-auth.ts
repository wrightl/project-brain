/**
 * Wrapper around fetch that handles session expiration (401 responses)
 * and redirects to login page
 */
export async function fetchWithAuth(
    input: RequestInfo | URL,
    init?: RequestInit
): Promise<Response> {
    const response = await fetch(input, init);

    // Handle 401 Unauthorized - session expired
    if (response.status === 401 || response.headers.get('X-Session-Expired') === 'true') {
        const currentPath = typeof window !== 'undefined' ? window.location.pathname : '/app';
        if (typeof window !== 'undefined') {
            window.location.href = `/auth/login?returnTo=${encodeURIComponent(currentPath)}`;
            // Return a promise that never resolves to prevent further execution
            return new Promise(() => {});
        }
    }

    return response;
}

