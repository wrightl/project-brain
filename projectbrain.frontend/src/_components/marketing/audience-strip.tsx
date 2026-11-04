import {
    BoltIcon,
    BriefcaseIcon,
    PuzzlePieceIcon,
    UserGroupIcon,
} from '@heroicons/react/24/outline';

const AUDIENCES = [
    {
        icon: BoltIcon,
        title: 'ADHD founders',
        body: 'Small daily goals, gentle streaks, and a buddy that helps you start — not another app that nags.',
    },
    {
        icon: PuzzlePieceIcon,
        title: 'Autistic business owners',
        body: 'Plain language, predictable screens, and the level of detail you ask for. Nothing changes without your say-so.',
    },
    {
        icon: BriefcaseIcon,
        title: 'Solo founders & freelancers',
        body: 'Someone to think out loud with between client work, marketing, and admin — at any hour.',
    },
    {
        icon: UserGroupIcon,
        title: 'Coaches',
        body: 'Find clients who need your specialism, and message them in one place alongside the rest of your clients.',
    },
] as const;

export function AudienceStrip() {
    return (
        <section className="border-y border-white/10 bg-navy px-6 py-14 text-white">
            <div className="mx-auto max-w-6xl">
                <p className="text-center text-sm tracking-widest text-brand uppercase">
                    Who we built this for
                </p>
                <h2 className="mt-3 text-center font-display text-3xl font-normal tracking-tight md:text-4xl">
                    Entrepreneurs who think differently — and the coaches who
                    back them
                </h2>
                <p className="mx-auto mt-4 max-w-2xl text-center text-white/70">
                    Running a business asks for planning, focus, and follow-up
                    every single day. ProjectBrain is support shaped around
                    neurodivergent strengths, not a productivity system you have
                    to bend yourself to fit.
                </p>
                <div className="mt-10 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
                    {AUDIENCES.map(({ icon: Icon, title, body }) => (
                        <div
                            key={title}
                            className="rounded-2xl border border-white/10 bg-white/5 p-6"
                        >
                            <Icon className="size-6 text-brand" aria-hidden />
                            <h3 className="mt-4 font-display text-lg font-semibold">{title}</h3>
                            <p className="mt-2 text-sm leading-relaxed text-white/70">{body}</p>
                        </div>
                    ))}
                </div>
            </div>
        </section>
    );
}
