import {
    AdjustmentsHorizontalIcon,
    ChatBubbleBottomCenterTextIcon,
    SwatchIcon,
} from '@heroicons/react/24/outline';

const POINTS = [
    {
        icon: ChatBubbleBottomCenterTextIcon,
        title: 'Answers your way',
        body: 'Tell us whether you like step-by-step instructions, visuals, or the short version. Your buddy adjusts how it explains things to match.',
    },
    {
        icon: AdjustmentsHorizontalIcon,
        title: 'Motivation that fits',
        body: 'Small goals, reminders, accountability, or rewards — pick what actually gets you moving, and how you like wins celebrated.',
    },
    {
        icon: SwatchIcon,
        title: 'A screen you can live with',
        body: 'Choose a theme that’s easy on your eyes, including a dark mode, and change it whenever you like.',
    },
] as const;

export function CalmFocusSection() {
    return (
        <section id="focus" className="scroll-mt-20 border-y border-line bg-surface px-6 py-16 text-navy md:py-20">
            <div className="mx-auto max-w-6xl">
                <p className="text-center text-sm tracking-widest text-navy/50 uppercase">
                    Built around you
                </p>
                <h2 className="mt-3 text-center font-display text-3xl font-normal tracking-tight md:text-4xl">
                    Support without the noise
                </h2>
                <p className="mx-auto mt-4 max-w-2xl text-center text-muted">
                    What you share during onboarding shapes every conversation,
                    so ProjectBrain works with your attention instead of
                    fighting it.
                </p>
                <ul className="mx-auto mt-12 grid max-w-5xl gap-10 md:grid-cols-3">
                    {POINTS.map(({ icon: Icon, title, body }) => (
                        <li key={title}>
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
