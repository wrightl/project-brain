const STEPS = [
    {
        n: '01',
        title: 'Tell us about you',
        body: 'A short onboarding about your business, your strengths, what you find hard, and how you like to learn. Skip anything you’d rather not share.',
    },
    {
        n: '02',
        title: 'Find a daily rhythm',
        body: 'Set three goals each morning, talk things through with your buddy when you’re stuck, and jot a journal entry at the end of the day.',
    },
    {
        n: '03',
        title: 'Bring in a coach',
        body: 'When you want a human perspective, search for a coach who specialises in what you need and connect in the app.',
    },
] as const;

export function HowItWorks() {
    return (
        <section id="how" className="scroll-mt-20 bg-surface px-6 py-16 text-navy md:py-20">
            <div className="mx-auto max-w-6xl">
                <h2 className="text-center font-display text-3xl font-normal tracking-tight md:text-4xl">
                    How it works
                </h2>
                <p className="mx-auto mt-4 max-w-2xl text-center text-muted">
                    A few minutes a day beats a perfect plan you never open
                    again.
                </p>
                <ol className="mt-12 grid gap-8 md:grid-cols-3">
                    {STEPS.map((step) => (
                        <li key={step.n} className="relative rounded-2xl border border-line p-6">
                            <span className="font-display text-3xl text-periwinkle">{step.n}</span>
                            <h3 className="mt-3 font-display text-xl font-semibold">{step.title}</h3>
                            <p className="mt-2 text-sm leading-relaxed text-muted">{step.body}</p>
                        </li>
                    ))}
                </ol>
            </div>
        </section>
    );
}
