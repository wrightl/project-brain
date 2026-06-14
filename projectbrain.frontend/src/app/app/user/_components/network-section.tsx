import Image from 'next/image';
import Link from 'next/link';
import { UsersIcon } from '@heroicons/react/24/outline';
import { ConnectionService } from '@/_services/connection-service';
import { CoachService } from '@/_services/coach-service';

type NetworkCoach = {
    connectionId: string;
    coachProfileId?: string;
    fullName: string;
    bio?: string | null;
    imageUrl?: string | null;
};

function initials(name: string) {
    return name
        .split(' ')
        .filter(Boolean)
        .slice(0, 2)
        .map((p) => p[0]?.toUpperCase())
        .join('');
}

export default async function NetworkSection() {
    const connections = await ConnectionService.getConnections({
        page: 1,
        pageSize: 50,
    });

    const accepted = connections.items.filter((c) => c.status === 'accepted');

    const coachProfileIds = accepted
        .map((connection) => connection.coachProfileId)
        .filter((id): id is string => Boolean(id));

    const summaries =
        coachProfileIds.length > 0
            ? await CoachService.getCoachSummaries(coachProfileIds)
            : {};

    const coaches: NetworkCoach[] = accepted.map((connection) => {
        const coachProfileId = connection.coachProfileId;
        const summary = coachProfileId ? summaries[coachProfileId] : undefined;

        return {
            connectionId: connection.id,
            coachProfileId,
            fullName:
                summary?.fullName ?? connection.coachName ?? 'Coach',
            bio: summary?.bio ?? null,
            imageUrl: summary?.imageUrl ?? null,
        };
    });

    return (
        <section className="relative overflow-hidden rounded-lg bg-white p-6 shadow border border-gray-300">
            <div
                aria-hidden="true"
                className="pointer-events-none absolute inset-0 opacity-[0.06]"
                style={{ background: 'var(--aqua-emerald-gradient)' }}
            />

            <div className="relative">
                <div className="flex items-start justify-between gap-4">
                    <div className="flex items-start gap-3">
                        <div className="mt-0.5 flex h-10 w-10 items-center justify-center rounded-md bg-[color:var(--light-aluminium)]">
                            <UsersIcon className="h-5 w-5 text-[color:var(--aqua)]" />
                        </div>

                        <div>
                            <h2 className="text-xl font-semibold text-gray-900">
                                Your network
                            </h2>
                            <p className="mt-1 text-sm text-gray-600">
                                Coaches you’re connected with.
                            </p>
                        </div>
                    </div>

                    <Link
                        href="/app/user/find-coaches"
                        className="inline-flex items-center gap-2 text-sm font-medium text-indigo-600 hover:text-indigo-700"
                    >
                        <UsersIcon className="h-5 w-5" />
                        Find coaches
                    </Link>
                </div>

                {coaches.length === 0 ? (
                    <div className="mt-6 rounded-md border border-dashed border-gray-300 p-6">
                        <p className="text-sm text-gray-700">
                            No coaches in your network yet.
                        </p>
                        <Link
                            href="/app/user/find-coaches"
                            className="mt-3 inline-flex items-center gap-2 rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
                        >
                            <UsersIcon className="h-5 w-5" />
                            Find a coach
                        </Link>
                    </div>
                ) : (
                    <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                        {coaches.map((coach) => (
                            <div
                                key={coach.connectionId}
                                className="rounded-lg border border-gray-200 p-5"
                            >
                                <div className="flex items-start gap-4">
                                    {coach.imageUrl ? (
                                        <Image
                                            src={coach.imageUrl}
                                            alt={coach.fullName}
                                            width={48}
                                            height={48}
                                            className="h-12 w-12 rounded-full object-cover border border-gray-200"
                                        />
                                    ) : (
                                        <div className="h-12 w-12 rounded-full bg-[color:var(--light-aluminium)] flex items-center justify-center border border-[color:var(--aluminium)] text-[color:var(--aqua)] font-semibold">
                                            {initials(coach.fullName)}
                                        </div>
                                    )}

                                    <div className="min-w-0">
                                        <p className="text-sm font-semibold text-gray-900">
                                            {coach.fullName}
                                        </p>
                                        <p className="mt-1 text-sm text-gray-600 line-clamp-3">
                                            {coach.bio ??
                                                'Coach profile details coming soon.'}
                                        </p>
                                    </div>
                                </div>

                                <div className="mt-4 flex items-center gap-3">
                                    <Link
                                        href={`/app/user/messages/${coach.connectionId}`}
                                        className="inline-flex flex-1 items-center justify-center rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700"
                                    >
                                        Message
                                    </Link>

                                    {coach.coachProfileId ? (
                                        <Link
                                            href={`/app/user/coaches/${coach.coachProfileId}`}
                                            className="inline-flex flex-1 items-center justify-center rounded-md bg-white px-3 py-2 text-sm font-medium text-indigo-700 ring-1 ring-inset ring-indigo-200 hover:bg-indigo-50"
                                        >
                                            View profile
                                        </Link>
                                    ) : (
                                        <span className="flex-1 inline-flex items-center justify-center rounded-md bg-gray-100 px-3 py-2 text-sm font-medium text-gray-400 cursor-not-allowed">
                                            View profile
                                        </span>
                                    )}
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </section>
    );
}
