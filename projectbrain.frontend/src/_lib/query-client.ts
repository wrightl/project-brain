'use client';

import { QueryClient } from '@tanstack/react-query';
import { ApiClientError } from './api-client';

export const QUERY_RETRY_MAX = 6;

function isStartupUnavailable(error: unknown): boolean {
    return error instanceof ApiClientError && error.status === 503;
}

function isRetryableQueryError(error: unknown): boolean {
    if (error instanceof ApiClientError) {
        return error.status === 503 || error.status >= 500;
    }

    return true;
}

/**
 * Queries may retry transient 5xx/network failures. Mutations must not:
 * a 500 or dropped connection can happen after the server already committed,
 * and retrying would duplicate journal entries, goals, connections, etc.
 * The startup gate returns 503 before handlers run, so that status is safe.
 */
export function shouldRetryQuery(failureCount: number, error: unknown): boolean {
    if (failureCount >= QUERY_RETRY_MAX) {
        return false;
    }

    return isRetryableQueryError(error);
}

export function shouldRetryMutation(failureCount: number, error: unknown): boolean {
    if (failureCount >= QUERY_RETRY_MAX) {
        return false;
    }

    return isStartupUnavailable(error);
}

export function retryDelay(attemptIndex: number, error: unknown): number {
    const baseMs = 2000;
    const maxMs = 15000;
    const exponential = Math.min(baseMs * 2 ** attemptIndex, maxMs);

    if (isStartupUnavailable(error)) {
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
                // Retry failed reads (cold start / SQL resume)
                retry: shouldRetryQuery,
                retryDelay,
                // Refetch on window focus
                refetchOnWindowFocus: false,
                // Refetch on reconnect
                refetchOnReconnect: true,
            },
            mutations: {
                // Only retry startup-gate 503s; never retry 5xx/network after a possible commit
                retry: shouldRetryMutation,
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
