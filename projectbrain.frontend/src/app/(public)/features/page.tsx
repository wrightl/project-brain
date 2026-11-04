import type { Metadata } from 'next';
import {
    BookOpenIcon,
    ChatBubbleLeftRightIcon,
    CheckCircleIcon,
    CheckIcon,
    CloudArrowUpIcon,
    LightBulbIcon,
    LockClosedIcon,
    MagnifyingGlassIcon,
    MicrophoneIcon,
    ShieldCheckIcon,
    UserGroupIcon,
} from '@heroicons/react/24/outline';
import { FinalCta } from '@/_components/marketing/final-cta';

export const metadata: Metadata = {
    title: 'Features',
    description:
        'Everything in ProjectBrain: an AI coaching buddy, three daily goals, a journal, coping strategies, voice notes, your own documents, and real coaches who understand neurodivergence.',
};

const USER_FEATURES = [
    {
        name: 'AI coaching buddy',
        icon: ChatBubbleLeftRightIcon,
        description:
            'Talk through what’s on your mind and get a clear next step. Shaped by what you shared during onboarding — your business, strengths, and how you like to learn.',
        details: [
            'Explains things the way you prefer: short, detailed, or step by step',
            'Sets goals, writes journal entries, or messages your coach — after you confirm',
            'Uses your uploaded documents when it answers',
            'Talk instead of type on paid plans',
        ],
    },
    {
        name: 'Daily goals',
        icon: CheckCircleIcon,
        description:
            'Up to three goals a day, so the list stays doable. Plan ahead when you have the energy, and let unfinished goals carry over without the guilt.',
        details: [
            'Three goals a day, by design',
            'Plan up to a week at a time, up to 30 days ahead',
            'See what slipped and pick it back up',
            'Streaks and achievements that build as you go',
        ],
    },
    {
        name: 'Journal',
        icon: BookOpenIcon,
        description:
            'Write a few lines about your day. Entries are summarised and tagged so patterns are easy to spot — for you and your buddy.',
        details: [
            'AI summary for each entry',
            'Tags to find entries by theme',
            'Journal streaks',
            'Ask your buddy to write an entry from a conversation',
        ],
    },
    {
        name: 'Coping strategies',
        icon: LightBulbIcon,
        description:
            'Keep a personal list of what helps when things get hard, and rate how well each one works so the best ones rise to the top.',
        details: [
            'Save strategies in your own words',
            'Rate them over time',
            'Your buddy suggests them when they’re relevant',
        ],
    },
    {
        name: 'Voice notes',
        icon: MicrophoneIcon,
        description:
            'Capture a thought out loud, wherever you are. Notes are transcribed so you can find them later.',
        details: ['Record in the browser', 'Automatic transcription', 'Play back any time'],
    },
    {
        name: 'Your documents',
        icon: CloudArrowUpIcon,
        description:
            'Upload the material that matters to your business — plans, pricing, notes from sessions — and your buddy will draw on it.',
        details: [
            'Upload common file types',
            'Organise your personal library',
            'Remove a document whenever you like',
        ],
    },
];

const COACH_FEATURES = [
    {
        name: 'Find the right coach',
        icon: MagnifyingGlassIcon,
        description:
            'Search by location and specialism, read profiles and ratings, and check availability before you reach out.',
    },
    {
        name: 'Connect and message',
        icon: UserGroupIcon,
        description:
            'Send a connection request, and once accepted, message each other in the app. Your buddy can pass a note on for you too.',
    },
    {
        name: 'For coaches',
        icon: ChatBubbleLeftRightIcon,
        description:
            'Build a profile with your specialisms, set your availability, find people looking for support, and manage clients and messages in one place.',
    },
];

const TRUST_FEATURES = [
    {
        name: 'Memory you control',
        icon: LightBulbIcon,
        description:
            'Your buddy remembers things so you don’t have to repeat yourself. Ask what it knows about you at any time, and tell it to forget.',
    },
    {
        name: 'Secure sign-in',
        icon: LockClosedIcon,
        description:
            'Accounts are protected by Auth0, and everything travels over an encrypted connection.',
    },
    {
        name: 'Your data, on request',
        icon: ShieldCheckIcon,
        description:
            'Download a copy of your profile, preferences, and history from your preferences, or ask us to delete your account and data.',
    },
];

function FeatureGrid({
    items,
}: {
    items: { name: string; icon: typeof CheckIcon; description: string; details?: string[] }[];
}) {
    return (
        <ul className="mt-12 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {items.map(({ name, icon: Icon, description, details }) => (
                <li key={name} className="rounded-2xl border border-line bg-surface p-6">
                    <Icon className="size-6 text-navy" aria-hidden />
                    <h3 className="mt-4 font-display text-lg font-semibold">{name}</h3>
                    <p className="mt-2 text-sm leading-relaxed text-muted">{description}</p>
                    {details && (
                        <ul className="mt-4 space-y-2 border-t border-line pt-4">
                            {details.map((detail) => (
                                <li key={detail} className="flex gap-2 text-sm">
                                    <CheckIcon className="mt-0.5 size-4 shrink-0 text-emerald-700" aria-hidden />
                                    {detail}
                                </li>
                            ))}
                        </ul>
                    )}
                </li>
            ))}
        </ul>
    );
}

function SectionHeading({ title, body }: { title: string; body: string }) {
    return (
        <div className="text-center">
            <h2 className="font-display text-3xl font-normal tracking-tight md:text-4xl">{title}</h2>
            <p className="mx-auto mt-4 max-w-2xl text-muted">{body}</p>
        </div>
    );
}

export default function FeaturesPage() {
    return (
        <>
            <section className="hero-dots px-6 pt-16 pb-16 text-white md:pt-20 md:pb-24">
                <div className="mx-auto max-w-3xl text-center">
                    <p className="text-sm tracking-widest text-brand uppercase">Features</p>
                    <h1 className="mt-4 font-display text-4xl leading-tight font-normal tracking-tight sm:text-5xl md:text-6xl">
                        Everything in ProjectBrain
                    </h1>
                    <p className="mx-auto mt-5 max-w-xl text-lg text-white/75">
                        Daily support from an AI buddy, the tools to keep track
                        of what works, and real coaches when you want a human
                        perspective.
                    </p>
                </div>
            </section>

            <section className="bg-wash px-6 py-16 text-navy md:py-24">
                <div className="mx-auto max-w-6xl">
                    <SectionHeading
                        title="For your day to day"
                        body="Small, steady support for starting, remembering, and following through."
                    />
                    <FeatureGrid items={USER_FEATURES} />
                </div>
            </section>

            <section className="bg-surface px-6 py-16 text-navy md:py-24">
                <div className="mx-auto max-w-6xl">
                    <SectionHeading
                        title="Coaches who understand"
                        body="AI for the everyday, people for the moments that need one."
                    />
                    <FeatureGrid items={COACH_FEATURES} />
                </div>
            </section>

            <section className="border-t border-line bg-wash px-6 py-16 text-navy md:py-24">
                <div className="mx-auto max-w-6xl">
                    <SectionHeading
                        title="Privacy and trust"
                        body="You share a lot with a coaching buddy. Here’s how we look after it."
                    />
                    <FeatureGrid items={TRUST_FEATURES} />
                    <p className="mx-auto mt-10 max-w-3xl text-center text-sm text-muted">
                        ProjectBrain is everyday support, not a medical or
                        crisis service. If you’re in crisis, please contact
                        local emergency services or a crisis line.
                    </p>
                </div>
            </section>

            <FinalCta
                heading="Start with the free plan."
                body="The AI buddy, daily goals, journal, and coach connections — free, with monthly limits. Upgrade when you need more."
            />
        </>
    );
}
