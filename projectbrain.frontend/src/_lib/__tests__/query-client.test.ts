import { ApiClientError } from '@/_lib/api-client';
import {
    retryDelay,
    shouldRetryMutation,
    shouldRetryQuery,
} from '@/_lib/query-client';

describe('query-client retry policy', () => {
    describe('shouldRetryQuery', () => {
        it('retries 503 and 5xx ApiClientError until the max', () => {
            expect(shouldRetryQuery(0, new ApiClientError(503, 'starting'))).toBe(
                true,
            );
            expect(shouldRetryQuery(5, new ApiClientError(500, 'boom'))).toBe(
                true,
            );
            expect(shouldRetryQuery(6, new ApiClientError(503, 'starting'))).toBe(
                false,
            );
        });

        it('does not retry 4xx ApiClientError', () => {
            expect(shouldRetryQuery(0, new ApiClientError(400, 'bad'))).toBe(
                false,
            );
            expect(shouldRetryQuery(0, new ApiClientError(401, 'auth'))).toBe(
                false,
            );
            expect(shouldRetryQuery(0, new ApiClientError(404, 'missing'))).toBe(
                false,
            );
        });

        it('retries network/unknown errors', () => {
            expect(shouldRetryQuery(0, new TypeError('Failed to fetch'))).toBe(
                true,
            );
            expect(shouldRetryQuery(0, new Error('timeout'))).toBe(true);
        });
    });

    describe('shouldRetryMutation', () => {
        it('retries only startup-gate 503s', () => {
            expect(
                shouldRetryMutation(0, new ApiClientError(503, 'starting')),
            ).toBe(true);
            expect(
                shouldRetryMutation(5, new ApiClientError(503, 'starting')),
            ).toBe(true);
            expect(
                shouldRetryMutation(6, new ApiClientError(503, 'starting')),
            ).toBe(false);
        });

        it('does not retry 5xx after a possible commit', () => {
            expect(
                shouldRetryMutation(0, new ApiClientError(500, 'boom')),
            ).toBe(false);
            expect(
                shouldRetryMutation(0, new ApiClientError(502, 'gateway')),
            ).toBe(false);
        });

        it('does not retry network errors that may have already committed', () => {
            expect(
                shouldRetryMutation(0, new TypeError('Failed to fetch')),
            ).toBe(false);
            expect(shouldRetryMutation(0, new Error('Failed to create journal entry'))).toBe(
                false,
            );
        });
    });

    describe('retryDelay', () => {
        it('uses at least 5s for 503 to match Retry-After', () => {
            expect(retryDelay(0, new ApiClientError(503, 'starting'))).toBe(5000);
        });

        it('uses exponential backoff for other errors', () => {
            expect(retryDelay(0, new ApiClientError(500, 'boom'))).toBe(2000);
            expect(retryDelay(3, new ApiClientError(500, 'boom'))).toBe(15000);
        });
    });
});
