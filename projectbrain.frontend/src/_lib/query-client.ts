'use client';

import { QueryClient } from '@tanstack/react-query';

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
                // Retry failed requests
                retry: 1,
                // Refetch on window focus
                refetchOnWindowFocus: false,
                // Refetch on reconnect
                refetchOnReconnect: true,
            },
            mutations: {
                // Retry failed mutations
                retry: 1,
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

