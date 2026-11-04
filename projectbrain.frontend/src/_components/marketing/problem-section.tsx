import {
    ArrowPathIcon,
    QueueListIcon,
    UserIcon,
} from '@heroicons/react/24/outline';

const PAINS = [
    {
        icon: QueueListIcon,
        title: 'A to-do list that only grows',
        body: 'Everything feels urgent, so nothing gets started. The list becomes one more thing to feel bad about.',
    },
    {
        icon: ArrowPathIcon,
        title: 'Starting from scratch every time',
        body: 'You worked out what helps last month — the walk before a call, the notes the night before — and then forgot.',
    },
    {
        icon: UserIcon,
        title: 'Doing it all alone',
        body: 'Generic business advice assumes a brain like everyone else’s. Coaches who get it are hard to find.',
    },
] as const;

export function ProblemSection() {
    return (
        <section className="bg-wash px-6 py-16 text-navy md:py-20">
            <div className="mx-auto max-w-6xl">
                <h2 className="text-center font-display text-3xl font-normal tracking-tight md:text-4xl">
                    Sound familiar?
                </h2>
                <p className="mx-auto mt-4 max-w-2xl text-center text-muted">
                    Most productivity tools are built for brains that already
                    find planning easy. If you’ve tried a few and given up, it
                    probably wasn’t you.
                </p>
                <div className="mt-12 grid gap-6 md:grid-cols-3">
                    {PAINS.map(({ icon: Icon, title, body }) => (
                        <div
                            key={title}
                            className="rounded-2xl border border-line bg-surface p-6 shadow-sm"
                        >
                            <Icon className="size-6 text-navy" aria-hidden />
                            <h3 className="mt-4 font-display text-lg font-semibold">{title}</h3>
                            <p className="mt-2 text-sm leading-relaxed text-muted">{body}</p>
                        </div>
                    ))}
                </div>
                <p className="mx-auto mt-10 max-w-2xl text-center text-base font-medium text-navy">
                    ProjectBrain puts a coaching buddy, small daily goals, your
                    journal, and real coaches in one calm place.
                </p>
            </div>
        </section>
    );
}
