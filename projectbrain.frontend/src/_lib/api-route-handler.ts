import { NextRequest, NextResponse } from 'next/server';
import { BackendApiError, SessionExpiredError } from './backend-api';
import { auth0 } from './auth';

type RouteHandler<T> = (
    req: NextRequest,
    context?: any
) => Promise<T | NextResponse> | T | NextResponse;

export function createApiRoute<T>(handler: RouteHandler<T>) {
    const wrapped = auth0.withApiAuthRequired(async function (
        req: Request | NextRequest,
        context?: unknown
    ): Promise<NextResponse> {
        try {
            const result = await handler(req as NextRequest, context);
            // If result is already a Response, return it directly
            if (result instanceof NextResponse) {
                return result;
            }
            if (result instanceof Response) {
                return NextResponse.json(await result.json());
            }
            // Otherwise, wrap the result in NextResponse.json
            return NextResponse.json(result as T);
        } catch (error) {
            console.error('API route error:', error);

            // Handle session expiration - return 401 with special header
            if (error instanceof SessionExpiredError) {
                return NextResponse.json(
                    { error: 'Session expired', code: 'SESSION_EXPIRED' },
                    {
                        status: 401,
                        headers: {
                            'X-Session-Expired': 'true',
                        },
                    }
                );
            }

            if (error instanceof BackendApiError) {
                return NextResponse.json(
                    { error: error.message, details: error.details },
                    { status: error.status }
                );
            }

            return NextResponse.json(
                { error: 'Internal server error' },
                { status: 500 }
            );
        }
    });

    // Type assertion to satisfy Next.js route handler types
    return wrapped as (
        req: NextRequest,
        context?: unknown
    ) => Promise<NextResponse>;
}
