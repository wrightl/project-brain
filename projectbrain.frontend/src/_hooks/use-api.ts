'use client';

import { ApiClientError } from '@/_lib/api-client';
import { useState, useCallback } from 'react';

interface UseApiState<T> {
    data: T | null;
    loading: boolean;
    error: string | null;
}

interface UseApiReturn<T> extends UseApiState<T> {
    execute: (...args: unknown[]) => Promise<T | null>;
    reset: () => void;
}

export function useApi<T>(
    apiFunction: (...args: unknown[]) => Promise<T>
): UseApiReturn<T> {
    const [state, setState] = useState<UseApiState<T>>({
        data: null,
        loading: false,
        error: null,
    });

    const execute = useCallback(
        async (...args: unknown[]) => {
            setState({ data: null, loading: true, error: null });

            try {
                const result = await apiFunction(...args);
                setState({ data: result, loading: false, error: null });
                return result;
            } catch (error) {
                const errorMessage =
                    error instanceof ApiClientError
                        ? error.message
                        : 'An unexpected error occurred';

                setState({ data: null, loading: false, error: errorMessage });
                return null;
            }
        },
        [apiFunction]
    );

    const reset = useCallback(() => {
        setState({ data: null, loading: false, error: null });
    }, []);

    return { ...state, execute, reset };
}
