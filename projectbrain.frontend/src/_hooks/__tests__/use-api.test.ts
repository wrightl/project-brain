import { renderHook, waitFor } from '@/_lib/test-utils';
import { useApi } from '@/_hooks/use-api';
import { ApiClientError } from '@/_lib/api-client';

// Mock the apiClient
jest.mock('@/_lib/api-client', () => ({
    apiClient: jest.fn(),
    ApiClientError: class extends Error {
        constructor(
            public status: number,
            public message: string,
            public details?: unknown
        ) {
            super(message);
            this.name = 'ApiClientError';
        }
    },
}));

describe('useApi', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('should initialize with null data, loading false, and no error', () => {
        const { result } = renderHook(() =>
            useApi(() => Promise.resolve('test'))
        );

        expect(result.current.data).toBeNull();
        expect(result.current.loading).toBe(false);
        expect(result.current.error).toBeNull();
    });

    it('should set loading to true when executing', async () => {
        const mockApiFunction = jest.fn(() => Promise.resolve('success'));
        const { result } = renderHook(() => useApi(mockApiFunction));

        const executePromise = result.current.execute();

        expect(result.current.loading).toBe(true);

        await executePromise;

        await waitFor(() => {
            expect(result.current.loading).toBe(false);
        });
    });

    it('should set data on successful execution', async () => {
        const mockApiFunction = jest.fn(() => Promise.resolve('success'));
        const { result } = renderHook(() => useApi(mockApiFunction));

        await result.current.execute();

        await waitFor(() => {
            expect(result.current.data).toBe('success');
            expect(result.current.error).toBeNull();
        });
    });

    it('should set error on failed execution', async () => {
        const mockError = new ApiClientError(500, 'Server error');
        const mockApiFunction = jest.fn(() => Promise.reject(mockError));
        const { result } = renderHook(() => useApi(mockApiFunction));

        await result.current.execute();

        await waitFor(() => {
            expect(result.current.error).toBe('Server error');
            expect(result.current.data).toBeNull();
        });
    });

    it('should reset state when reset is called', async () => {
        const mockApiFunction = jest.fn(() => Promise.resolve('success'));
        const { result } = renderHook(() => useApi(mockApiFunction));

        await result.current.execute();

        await waitFor(() => {
            expect(result.current.data).toBe('success');
        });

        result.current.reset();

        expect(result.current.data).toBeNull();
        expect(result.current.loading).toBe(false);
        expect(result.current.error).toBeNull();
    });
});

