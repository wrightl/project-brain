import { CheckIcon, XMarkIcon } from '@heroicons/react/24/outline';

const DIFFERENT = [
    'Built for neurodivergent entrepreneurs specifically — not a general wellbeing app with a business tab',
    'Three goals a day, by design — small enough to start, with no backlog shame',
    'An AI buddy that asks before it acts — it suggests goals, journal entries and messages, and you confirm',
    'Memory you can inspect — ask what it remembers, and tell it to forget',
    'AI between sessions, humans when it matters — real coaches, found by specialism and location',
    'Your data stays yours — download a copy from your preferences, or ask us to delete it',
] as const;

export function DifferentiatorSection() {
    return (
        <section className="bg-navy px-6 py-16 text-white md:py-20">
            <div className="mx-auto max-w-6xl">
                <h2 className="text-center font-display text-3xl font-normal tracking-tight md:text-4xl">
                    What makes it different
                </h2>
                <p className="mx-auto mt-4 max-w-2xl text-center text-white/70">
                    Everyday support for running a business — not a replacement
                    for therapy, diagnosis, or medical care.
                </p>
                <ul className="mx-auto mt-10 max-w-3xl space-y-4">
                    {DIFFERENT.map((item) => (
                        <li
                            key={item}
                            className="flex gap-3 rounded-xl border border-white/10 bg-white/5 px-5 py-4 text-sm leading-relaxed"
                        >
                            <CheckIcon className="mt-0.5 size-5 shrink-0 text-brand" aria-hidden />
                            <span>{item}</span>
                        </li>
                    ))}
                </ul>
            </div>
        </section>
    );
}

const FOR = [
    'Neurodivergent founders, freelancers, and small business owners — diagnosed or not',
    'People who find starting, planning, or following through harder than the work itself',
    'Anyone who wants a coach who understands neurodivergence, or support between sessions',
    'Coaches who specialise in supporting neurodivergent people',
] as const;

const NOT_FOR = [
    'Crisis or emergency support — please contact local emergency services or a crisis line',
    'Clinical diagnosis, therapy, or medication advice',
    'Teams looking for a full project management tool',
] as const;

export function WhoItsFor() {
    return (
        <section id="who" className="scroll-mt-20 bg-wash px-6 py-16 text-navy md:py-20">
            <div className="mx-auto max-w-6xl">
                <h2 className="text-center font-display text-3xl font-normal tracking-tight md:text-4xl">
                    Who it&apos;s for
                </h2>
                <div className="mt-12 grid gap-8 md:grid-cols-2">
                    <div className="rounded-2xl border border-line bg-surface p-8">
                        <h3 className="font-display text-xl font-semibold">A good fit if you…</h3>
                        <ul className="mt-6 space-y-3">
                            {FOR.map((item) => (
                                <li key={item} className="flex gap-3 text-sm">
                                    <CheckIcon className="mt-0.5 size-5 shrink-0 text-emerald-700" aria-hidden />
                                    {item}
                                </li>
                            ))}
                        </ul>
                    </div>
                    <div className="rounded-2xl border border-line bg-surface p-8">
                        <h3 className="font-display text-xl font-semibold">Not the right place for…</h3>
                        <ul className="mt-6 space-y-3">
                            {NOT_FOR.map((item) => (
                                <li key={item} className="flex gap-3 text-sm">
                                    <XMarkIcon className="mt-0.5 size-5 shrink-0 text-rose-700" aria-hidden />
                                    {item}
                                </li>
                            ))}
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    );
}
