import { type NextRequest } from 'next/server';
import { authMiddleware } from './_lib/auth';

export async function middleware(request: NextRequest) {
    const authRes = await authMiddleware(request);

    if (request.nextUrl.pathname.startsWith('/auth')) {
        // authentication routes
        return authRes;
    } else if (request.nextUrl.pathname === '/') {
        // home route - publicly accessible
        return authRes;
    }

    return authRes;
}

export const config = {
    matcher: [
        /*
         * Match all request paths except for the ones starting with:
         * - _next/static (static files)
         * - _next/image (image optimization files)
         * - favicon.ico, sitemap.xml, robots.txt (metadata files)
         */
        '/((?!_next/static|_next/image|favicon.ico|sitemap.xml|robots.txt).*)',
    ],
};
