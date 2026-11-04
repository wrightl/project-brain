import { twMerge } from 'tailwind-merge';
import {
    ChatMock,
    CoachesMock,
    GoalsMock,
    JournalMock,
    StrategiesMock,
} from './product-mocks';

type FeatureSplitProps = {
    eyebrow: string;
    title: string;
    body: string;
    visual: React.ReactNode;
    /** When true, the visual sits on the left on large screens. */
    reverse?: boolean;
};

function FeatureSplit({ eyebrow, title, body, visual, reverse = false }: FeatureSplitProps) {
    return (
        <div
            className={twMerge(
                'grid grid-cols-1 items-center gap-10 lg:grid-cols-2 lg:gap-16',
                reverse && 'lg:[&>*:first-child]:order-2'
            )}
        >
            <div>
                <p className="text-sm tracking-widest text-navy/50 uppercase">{eyebrow}</p>
                <h3 className="mt-2 font-display text-2xl font-normal tracking-tight md:text-3xl">
                    {title}
                </h3>
                <p className="mt-4 text-base leading-relaxed text-muted">{body}</p>
            </div>
            <div className="mx-auto w-full max-w-lg">{visual}</div>
        </div>
    );
}

const FEATURES: FeatureSplitProps[] = [
    {
        eyebrow: 'Coaching buddy',
        title: 'Think out loud, get a next step',
        body: 'Chat or talk to an AI buddy that knows your business, your strengths, and how you like information — short and simple or all the detail. It turns “I’m stuck” into one small thing you can do now.',
        visual: <ChatMock />,
    },
    {
        eyebrow: 'Daily goals',
        title: 'Three goals a day. That’s it.',
        body: 'Never more than three, so the list stays doable. Plan up to a week ahead, carry over what slipped without guilt, and watch a streak build. Your buddy can set them up straight from a conversation.',
        visual: <GoalsMock />,
        reverse: true,
    },
    {
        eyebrow: 'Journal',
        title: 'A journal that notices patterns',
        body: 'Write a few lines at the end of the day. Each entry is summarised and tagged, so your buddy can remind you what helped last time — and you can look back and see how far you’ve come.',
        visual: <JournalMock />,
    },
    {
        eyebrow: 'Coping strategies',
        title: 'Keep what works for you',
        body: 'Save the strategies that get you through overwhelm, a tough call, or a foggy afternoon. Rate how well they work, and your buddy will suggest the right one when it comes up again.',
        visual: <StrategiesMock />,
        reverse: true,
    },
    {
        eyebrow: 'Real coaches',
        title: 'A human in your corner',
        body: 'Search for coaches near you by specialism, see ratings and availability, and connect when you’re ready. Message them in the app, and ask your buddy to send them a note when something comes up between sessions.',
        visual: <CoachesMock />,
    },
];

export function FeatureSplits() {
    return (
        <section id="product" className="scroll-mt-20 bg-wash px-6 py-16 text-navy md:py-24">
            <div className="mx-auto max-w-6xl space-y-20 md:space-y-28">
                <div className="text-center">
                    <h2 className="font-display text-3xl font-normal tracking-tight md:text-4xl">
                        What you get
                    </h2>
                    <p className="mx-auto mt-4 max-w-2xl text-muted">
                        Built around how running a business actually feels day
                        to day — the starting, the remembering, and the
                        follow-through.
                    </p>
                </div>
                {FEATURES.map((feature) => (
                    <FeatureSplit key={feature.title} {...feature} />
                ))}
            </div>
        </section>
    );
}
