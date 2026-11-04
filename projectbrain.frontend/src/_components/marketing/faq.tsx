export type FaqItem = { q: string; a: string };

export const HOME_FAQS: FaqItem[] = [
    {
        q: 'Do I need a diagnosis to use ProjectBrain?',
        a: 'No. ProjectBrain is for anyone who identifies as neurodivergent or simply finds that standard productivity advice doesn’t fit how they work.',
    },
    {
        q: 'Is the AI a replacement for a coach or therapist?',
        a: 'No. Your buddy is there for the everyday — breaking tasks down, thinking things through, remembering what helped. For a human perspective, connect with a coach. ProjectBrain isn’t a medical or crisis service; if you’re in crisis, please contact local emergency services or a crisis line.',
    },
    {
        q: 'What does the AI remember about me?',
        a: 'It keeps notes from your onboarding and conversations so you don’t have to repeat yourself. You can ask it what it remembers at any time and tell it to forget specific things.',
    },
    {
        q: 'Will the AI do things without asking?',
        a: 'No. When your buddy wants to set goals, write a journal entry, or message your coach for you, it asks first and only goes ahead once you confirm.',
    },
    {
        q: 'Can I get my data out, or delete it?',
        a: 'Yes. You can download a copy of your profile, preferences and history from your preferences at any time, and ask us to delete your account and data.',
    },
    {
        q: 'How do coaches work?',
        a: 'Search for coaches by location and specialism, view their profiles and ratings, and send a connection request. Once connected, you can message each other in the app.',
    },
    {
        q: 'Is there a free plan?',
        a: 'Yes. The free plan includes the AI buddy, daily goals, journal, and coach connections with monthly limits. Paid plans remove the limits, add more storage, and let you talk to your buddy instead of typing. See pricing for details.',
    },
];

export function FaqSection({ faqs = HOME_FAQS }: { faqs?: FaqItem[] }) {
    return (
        <section className="bg-surface px-6 py-16 text-navy md:py-20">
            <div className="mx-auto max-w-3xl">
                <h2 className="text-center font-display text-3xl font-normal tracking-tight md:text-4xl">
                    Questions
                </h2>
                <div className="mt-10 space-y-3">
                    {faqs.map((item) => (
                        <details
                            key={item.q}
                            className="group rounded-2xl border border-line bg-wash/50 px-5 py-4 open:bg-wash"
                        >
                            <summary className="cursor-pointer list-none font-medium marker:content-none [&::-webkit-details-marker]:hidden">
                                <span className="flex items-center justify-between gap-4">
                                    {item.q}
                                    <span className="text-muted transition group-open:rotate-45" aria-hidden>
                                        +
                                    </span>
                                </span>
                            </summary>
                            <p className="mt-3 text-sm leading-relaxed text-muted">{item.a}</p>
                        </details>
                    ))}
                </div>
            </div>
        </section>
    );
}
