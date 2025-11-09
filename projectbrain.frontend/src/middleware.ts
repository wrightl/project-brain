import { type NextRequest } from 'next/server';
import { authMiddleware } from './_lib/auth';
// Removed UserService and auth0 import as logic is moved to /app/app/onboarding/check/page.tsx

export async function middleware(request: NextRequest) {
    const authRes = await authMiddleware(request);

    // If authMiddleware returned a redirect, it means the user is not authenticated
    // or needs to complete an Auth0 flow. Return its response directly.
    if (authRes.status === 302) {
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
