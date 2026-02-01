import { getSession } from '@/_lib/auth';
import AcceptReferralClient from './_components/accept-referral-client';

export default async function ReferralAcceptPage({
    searchParams,
}: {
    searchParams?:
        | Record<string, string | string[] | undefined>
        | Promise<Record<string, string | string[] | undefined>>;
}) {
    // Next.js may provide `searchParams` as a Promise (sync dynamic APIs).
    const resolvedSearchParams = await Promise.resolve(searchParams);
    const tokenParam = resolvedSearchParams?.token;
    const token =
        typeof tokenParam === 'string'
            ? tokenParam
            : Array.isArray(tokenParam)
                ? tokenParam[0] || ''
                : '';

    const session = await getSession();
    const isAuthenticated = !!session?.user;

    return (
        <AcceptReferralClient token={token} isAuthenticated={isAuthenticated} />
    );
}

