import {
    AcademicCapIcon,
    ChatBubbleLeftEllipsisIcon,
    CloudArrowUpIcon,
    LightBulbIcon,
    MicrophoneIcon,
    TrophyIcon,
} from '@heroicons/react/24/outline';

const MORE = [
    {
        icon: LightBulbIcon,
        title: 'It remembers, and you can see what',
        body: 'Your buddy keeps notes on what you’ve told it so you don’t repeat yourself. Ask what it remembers at any time, and tell it to forget anything.',
    },
    {
        icon: MicrophoneIcon,
        title: 'Voice notes',
        body: 'Talk instead of type. Record a thought on the go and have it transcribed, so ideas don’t get lost between meetings.',
    },
    {
        icon: CloudArrowUpIcon,
        title: 'Your own documents',
        body: 'Upload your business plan, pricing, or notes from a coach, and your buddy will use them when it answers.',
    },
    {
        icon: AcademicCapIcon,
        title: 'Quizzes and resources',
        body: 'Short check-ins that help you understand how you work, plus a library of resources to dip into.',
    },
    {
        icon: ChatBubbleLeftEllipsisIcon,
        title: 'Pick up where you left off',
        body: 'Every conversation is saved, so you can come back to an idea days later without starting again.',
    },
    {
        icon: TrophyIcon,
        title: 'Progress you can see',
        body: 'Goal and journal streaks and achievements, celebrated the way you choose — a quiet note or a proper fanfare.',
    },
] as const;

export function MoreFeatures() {
    return (
        <section className="bg-surface px-6 py-16 text-navy md:py-24">
            <div className="mx-auto max-w-6xl">
                <div className="text-center">
                    <h2 className="font-display text-3xl font-normal tracking-tight md:text-4xl">
                        Support that fits around you
                    </h2>
                    <p className="mx-auto mt-4 max-w-2xl text-muted">
                        The small things that make it easier to keep going on
                        the days that are harder than others.
                    </p>
                </div>
                <ul className="mt-12 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
                    {MORE.map(({ icon: Icon, title, body }) => (
                        <li key={title} className="rounded-2xl border border-line bg-wash/40 p-6">
                            <Icon className="size-6 text-navy" aria-hidden />
                            <h3 className="mt-4 font-display text-lg font-semibold">{title}</h3>
                            <p className="mt-2 text-sm leading-relaxed text-muted">{body}</p>
                        </li>
                    ))}
                </ul>
            </div>
        </section>
    );
}
