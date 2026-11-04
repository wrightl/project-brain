import {
    CheckCircleIcon,
    MapPinIcon,
    SparklesIcon,
    StarIcon,
} from '@heroicons/react/24/solid';

/*
 * Illustrative product mocks for the marketing pages. They stand in for
 * screenshots, so each one is a single labelled image to assistive tech.
 */

function MockFrame({
    label,
    title,
    children,
}: {
    label: string;
    title: string;
    children: React.ReactNode;
}) {
    return (
        <div
            role="img"
            aria-label={label}
            className="overflow-hidden rounded-2xl border border-line bg-surface text-navy shadow-sm"
        >
            <div aria-hidden className="flex items-center gap-2 border-b border-line bg-wash/60 px-4 py-2.5">
                <span className="size-2.5 rounded-full bg-brand" />
                <span className="size-2.5 rounded-full bg-periwinkle" />
                <span className="size-2.5 rounded-full bg-navy/20" />
                <span className="ml-2 text-xs tracking-widest text-navy/60 uppercase">
                    {title}
                </span>
            </div>
            <div aria-hidden className="p-5">
                {children}
            </div>
        </div>
    );
}

export function ChatMock() {
    return (
        <MockFrame
            title="Chat"
            label="Chat with the AI coaching buddy breaking a launch into three small steps"
        >
            <div className="space-y-3 text-sm">
                <p className="ml-auto max-w-[80%] rounded-2xl rounded-br-sm bg-navy px-4 py-2.5 text-white">
                    I need to launch my newsletter but I keep freezing.
                </p>
                <div className="max-w-[85%] rounded-2xl rounded-bl-sm bg-wash px-4 py-2.5">
                    <p>That&apos;s a big task — let&apos;s make it smaller. Three steps for today:</p>
                    <ol className="mt-2 list-decimal space-y-1 pl-5">
                        <li>Pick a name (10 minutes, no more)</li>
                        <li>Write three bullet points for issue one</li>
                        <li>Send it to one friend</li>
                    </ol>
                    <p className="mt-2">Want me to add these as today&apos;s goals?</p>
                </div>
                <p className="ml-auto w-fit rounded-2xl rounded-br-sm bg-navy px-4 py-2.5 text-white">
                    Yes please
                </p>
            </div>
        </MockFrame>
    );
}

const GOALS = [
    { text: 'Pick a newsletter name', done: true },
    { text: 'Draft three bullet points', done: true },
    { text: 'Send to one friend', done: false },
];

export function GoalsMock() {
    return (
        <MockFrame
            title="Today"
            label="Today's three goals, two ticked off, with a seven-day streak"
        >
            <div className="flex items-baseline justify-between">
                <p className="font-display text-lg">Three goals for today</p>
                <p className="text-xs text-muted">2 of 3 done</p>
            </div>
            <ul className="mt-4 space-y-2.5">
                {GOALS.map((goal) => (
                    <li
                        key={goal.text}
                        className="flex items-center gap-3 rounded-xl border border-line px-4 py-3 text-sm"
                    >
                        {goal.done ? (
                            <CheckCircleIcon className="size-5 shrink-0 text-navy" />
                        ) : (
                            <span className="size-5 shrink-0 rounded-full border-2 border-navy/25" />
                        )}
                        <span className={goal.done ? 'text-muted line-through' : ''}>
                            {goal.text}
                        </span>
                    </li>
                ))}
            </ul>
            <div className="mt-5 flex gap-1.5">
                {[1, 1, 1, 1, 1, 1, 0.6].map((opacity, i) => (
                    <span
                        key={i}
                        className="h-2 flex-1 rounded-full bg-brand"
                        style={{ opacity }}
                    />
                ))}
            </div>
            <p className="mt-2 text-xs text-muted">7-day streak</p>
        </MockFrame>
    );
}

export function JournalMock() {
    return (
        <MockFrame
            title="Journal"
            label="Journal entry with an AI summary and tags"
        >
            <p className="text-xs text-muted">Tuesday evening</p>
            <p className="mt-1 font-display text-lg">Pitch went better than I expected</p>
            <p className="mt-3 text-sm leading-relaxed text-muted">
                I was dreading it all week. Prepared notes the night before and
                took a walk first — that helped. Need to follow up with Sam on
                Thursday.
            </p>
            <div className="mt-4 flex gap-3 rounded-xl bg-wash px-4 py-3 text-sm">
                <SparklesIcon className="mt-0.5 size-5 shrink-0 text-navy" />
                <span>
                    <span className="block text-xs tracking-widest text-navy/60 uppercase">
                        Summary
                    </span>
                    Preparing early and moving first eased the nerves.
                </span>
            </div>
            <div className="mt-4 flex flex-wrap gap-2">
                {['confidence', 'preparation', 'follow-up'].map((tag) => (
                    <span
                        key={tag}
                        className="rounded-full border border-line px-3 py-1 text-xs"
                    >
                        {tag}
                    </span>
                ))}
            </div>
        </MockFrame>
    );
}

export function StrategiesMock() {
    return (
        <MockFrame
            title="Strategies"
            label="Saved coping strategies for overwhelm and focus"
        >
            <p className="font-display text-lg">What works for you</p>
            <ul className="mt-4 space-y-3 text-sm">
                {[
                    ['When overwhelmed', 'Write everything down, then pick just one thing.'],
                    ['Before a big call', 'Ten-minute walk and three prepared notes.'],
                    ['Losing focus', '25 minutes on, 5 off, phone in another room.'],
                ].map(([when, what]) => (
                    <li key={when} className="rounded-xl border border-line px-4 py-3">
                        <p className="text-xs tracking-widest text-navy/60 uppercase">{when}</p>
                        <p className="mt-1">{what}</p>
                    </li>
                ))}
            </ul>
        </MockFrame>
    );
}

export function CoachesMock() {
    return (
        <MockFrame
            title="Find a coach"
            label="Coach search results with specialisms, ratings and availability"
        >
            <ul className="space-y-3 text-sm">
                {[
                    ['Business coaching · ADHD', 'Available', 4.9],
                    ['Executive function · Autism', 'Available', 4.8],
                    ['Marketing · Dyslexia', 'Waitlist', 4.7],
                ].map(([specialism, status, rating]) => (
                    <li
                        key={specialism as string}
                        className="flex items-center gap-3 rounded-xl border border-line px-4 py-3"
                    >
                        <span className="size-10 shrink-0 rounded-full bg-periwinkle/60" />
                        <span className="min-w-0 flex-1">
                            <span className="block truncate">{specialism}</span>
                            <span className="mt-0.5 flex items-center gap-1 text-xs text-muted">
                                <MapPinIcon className="size-3.5" />
                                Within 10 miles
                                <StarIcon className="ml-2 size-3.5 text-navy" />
                                {rating}
                            </span>
                        </span>
                        <span
                            className={
                                status === 'Available'
                                    ? 'rounded-full bg-wash px-2.5 py-1 text-xs'
                                    : 'rounded-full border border-line px-2.5 py-1 text-xs text-muted'
                            }
                        >
                            {status}
                        </span>
                    </li>
                ))}
            </ul>
        </MockFrame>
    );
}
