import type { Metadata } from 'next';
import UserTiers from './_components/user-tiers';
import CoachTiers from './_components/coach-tiers';
import { buttonClasses, SIGNUP_HREF } from '@/_components/marketing/links';
import { FaqSection, type FaqItem } from '@/_components/marketing/faq';
import { FinalCta } from '@/_components/marketing/final-cta';

export const metadata: Metadata = {
    title: 'Pricing',
    description:
        'Start free with ProjectBrain. Pro and Ultimate plans remove the limits on your AI coaching buddy and coach connections. Coach plans available.',
};

const PRICING_FAQS: FaqItem[] = [
    {
        q: 'Is the free plan really free?',
        a: 'Yes. No card is needed to sign up, and the free plan doesn’t expire. You’ll only pay if you choose to upgrade.',
    },
    {
        q: 'How does the free trial work?',
        a: 'Pro plans for founders and coaches start with a 7-day free trial, so you can try them before you pay.',
    },
    {
        q: 'Can I change or cancel my plan?',
        a: 'Yes. Upgrade, downgrade, or cancel from the subscription page in the app at any time.',
    },
    {
        q: 'What counts as an AI query?',
        a: 'Each message you send to your coaching buddy counts as one query. Your usage so far is always visible in the app.',
    },
    {
        q: 'Do coaching sessions cost extra?',
        a: 'ProjectBrain plans cover the app. Any sessions you book are arranged between you and your coach.',
    },
];

export default function PricingPage() {
    return (
        <>
            <section className="hero-dots px-6 pt-16 pb-16 text-white md:pt-20 md:pb-24">
                <div className="mx-auto max-w-3xl text-center">
                    <p className="text-sm tracking-widest text-brand uppercase">Pricing</p>
                    <h1 className="mt-4 font-display text-4xl leading-tight font-normal tracking-tight sm:text-5xl md:text-6xl">
                        Start free. Upgrade when it helps.
                    </h1>
                    <p className="mx-auto mt-5 max-w-xl text-lg text-white/75">
                        The free plan includes your AI coaching buddy, daily
                        goals, journal, and coach connections. Paid plans
                        remove the limits. Save two months when you pay
                        annually.
                    </p>
                    <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
                        <a href={SIGNUP_HREF} className={buttonClasses('primary', 'px-6 py-2.5')}>
                            Sign up free
                        </a>
                    </div>
                </div>
            </section>

            <section className="bg-wash px-6 py-16 text-navy md:py-24">
                <div className="mx-auto max-w-6xl">
                    <div className="text-center">
                        <h2 className="font-display text-3xl font-normal tracking-tight md:text-4xl">
                            For founders
                        </h2>
                        <p className="mx-auto mt-4 max-w-2xl text-muted">
                            Simple plans with honest limits.
                        </p>
                    </div>
                    <UserTiers />
                </div>
            </section>

            <section id="coaches" className="scroll-mt-20 bg-surface px-6 py-16 text-navy md:py-24">
                <div className="mx-auto max-w-6xl">
                    <div className="text-center">
                        <h2 className="font-display text-3xl font-normal tracking-tight md:text-4xl">
                            For coaches
                        </h2>
                        <p className="mx-auto mt-4 max-w-2xl text-muted">
                            Be found by neurodivergent founders and manage your
                            clients in one place.
                        </p>
                    </div>
                    <CoachTiers />
                </div>
            </section>

            <FaqSection faqs={PRICING_FAQS} />
            <FinalCta
                heading="Start with the free plan."
                body="Set up your coaching buddy in a few minutes. No card needed."
            />
        </>
    );
}
