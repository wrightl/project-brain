import { NextRequest, NextResponse } from 'next/server';
import { BackendApiError, SessionExpiredError } from './backend-api';
import { auth0, requireRole } from './auth';
import { AppRoles } from './roles';

type RouteHandler<T> = (
    req: NextRequest,
    context?: any
) => Promise<T | NextResponse> | T | NextResponse;

type StreamingRouteHandler = (req: NextRequest) => Promise<Response>;

function wrapApiHandler<T>(handler: RouteHandler<T>) {
    return async function (
        req: Request | NextRequest,
        context?: unknown
    ): Promise<NextResponse> {
        try {
            const result = await handler(req as NextRequest, context);
            if (result instanceof NextResponse) {
                return result;
            }
            if (result instanceof Response) {
                return NextResponse.json(await result.json());
            }
            return NextResponse.json(result as T);
        } catch (error) {
            console.error('API route error:', error);

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
    };
}

export function createApiRoute<T>(handler: RouteHandler<T>) {
    const wrapped = auth0.withApiAuthRequired(wrapApiHandler(handler));
    return wrapped as (
        req: NextRequest,
        context?: unknown
    ) => Promise<NextResponse>;
}

export function createAdminApiRoute<T>(handler: RouteHandler<T>) {
    return createApiRoute<T>(async (req, context) => {
        const allowed = await requireRole(AppRoles.Admin);
        if (!allowed) {
            return NextResponse.json({ error: 'Forbidden' }, { status: 403 });
        }

        return handler(req, context);
    });
}

export function createStreamingApiRoute(handler: StreamingRouteHandler) {
    const wrapped = auth0.withApiAuthRequired(async (req: Request) => {
        try {
            return await handler(req as NextRequest);
        } catch (error) {
            console.error('Streaming API route error:', error);

            if (error instanceof SessionExpiredError) {
                return NextResponse.json(
                    { error: 'Session expired', code: 'SESSION_EXPIRED' },
                    { status: 401 }
                );
            }

            return NextResponse.json(
                { error: 'Internal server error' },
                { status: 500 }
            );
        }
    });

    return wrapped as (req: NextRequest) => Promise<Response>;
}
