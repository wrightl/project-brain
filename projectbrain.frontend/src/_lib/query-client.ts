'use client';

import { QueryClient } from '@tanstack/react-query';
import { ApiClientError } from './api-client';

function shouldRetry(failureCount: number, error: unknown): boolean {
    // Budget for Azure SQL serverless resume + Container Apps scale-from-0 (~60–90s)
    if (failureCount >= 6) {
        return false;
    }

    if (error instanceof ApiClientError) {
        return error.status === 503 || error.status >= 500;
    }

    return true;
}

function retryDelay(attemptIndex: number, error: unknown): number {
    const baseMs = 2000;
    const maxMs = 15000;
    const exponential = Math.min(baseMs * 2 ** attemptIndex, maxMs);

    if (error instanceof ApiClientError && error.status === 503) {
        // Align with API Retry-After default (5s) on startup gate
        return Math.max(exponential, 5000);
    }

    return exponential;
}

// Create a singleton QueryClient instance
// This ensures we have a single instance across the app
function makeQueryClient() {
    return new QueryClient({
        defaultOptions: {
            queries: {
                // Stale time: how long data is considered fresh
                staleTime: 60 * 1000, // 1 minute
                // Cache time: how long unused data stays in cache
                gcTime: 5 * 60 * 1000, // 5 minutes (formerly cacheTime)
                // Retry failed requests (cold start / SQL resume)
                retry: shouldRetry,
                retryDelay,
                // Refetch on window focus
                refetchOnWindowFocus: false,
                // Refetch on reconnect
                refetchOnReconnect: true,
            },
            mutations: {
                // Retry failed mutations for transient cold-start errors
                retry: shouldRetry,
                retryDelay,
            },
        },
    });
}

let browserQueryClient: QueryClient | undefined = undefined;

export function getQueryClient() {
    if (typeof window === 'undefined') {
        // Server: always make a new query client
        return makeQueryClient();
    } else {
        // Browser: use singleton pattern to keep the same query client
        if (!browserQueryClient) browserQueryClient = makeQueryClient();
        return browserQueryClient;
    }
}
