import { NextRequest, NextResponse } from 'next/server';
import { BackendApiError, SessionExpiredError } from './backend-api';
import { auth0 } from './auth';

type RouteHandler<T> = (
    req: NextRequest,
    context?: unknown
) => Promise<T> | NextResponse;

export function createApiRoute<T>(handler: RouteHandler<T>) {
    return auth0.withApiAuthRequired(async function (
        req: NextRequest,
        context?: unknown
    ) {
        //return async function (req: NextRequest, context?: unknown) {
        try {
            // const session = await auth0.getSession(); // Get session without req/res for App Router
            // if (!session || !session.user) {
            //     return NextResponse.json(
            //         { error: 'Unauthorized' },
            //         { status: 401 }
            //     );
            // }

            const result = await handler(req, context);
            // If result is already a Response, return it directly
            if (result instanceof NextResponse || result instanceof Response) {
                return result;
            }
            // Otherwise, wrap the result in NextResponse.json
            return NextResponse.json(result);
        } catch (error) {
            console.error('API route error:', error);

            // Handle session expiration - return 401 with special header
            if (error instanceof SessionExpiredError) {
                return NextResponse.json(
                    { error: 'Session expired', code: 'SESSION_EXPIRED' },
                    { 
                        status: 401,
                        headers: {
                            'X-Session-Expired': 'true'
                        }
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
}
