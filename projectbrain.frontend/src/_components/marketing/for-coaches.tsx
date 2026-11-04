import Link from 'next/link';
import {
    ChatBubbleLeftRightIcon,
    MagnifyingGlassIcon,
    StarIcon,
} from '@heroicons/react/24/outline';
import { buttonClasses, SIGNUP_HREF } from './links';

const POINTS = [
    {
        icon: MagnifyingGlassIcon,
        title: 'Be found by the right people',
        body: 'List your specialisms and location. Founders searching for support like yours can find you, and you can search for people looking for a coach.',
    },
    {
        icon: ChatBubbleLeftRightIcon,
        title: 'Clients and messages in one place',
        body: 'Accept connection requests, keep a list of your clients, and message them in the app instead of across email and chat apps.',
    },
    {
        icon: StarIcon,
        title: 'Build a reputation',
        body: 'Clients can rate their experience, and your availability status lets people know when you’re taking on new work.',
    },
] as const;

export function ForCoaches() {
    return (
        <section id="coaches" className="scroll-mt-20 bg-navy px-6 py-16 text-white md:py-20">
            <div className="mx-auto max-w-6xl">
                <p className="text-center text-sm tracking-widest text-brand uppercase">
                    For coaches
                </p>
                <h2 className="mt-3 text-center font-display text-3xl font-normal tracking-tight md:text-4xl">
                    Coach neurodivergent founders? Meet them here.
                </h2>
                <p className="mx-auto mt-4 max-w-2xl text-center text-white/70">
                    ProjectBrain gives your clients daily support between
                    sessions, and gives you a simple way to be found and stay in
                    touch.
                </p>
                <ul className="mx-auto mt-12 grid max-w-5xl gap-6 md:grid-cols-3">
                    {POINTS.map(({ icon: Icon, title, body }) => (
                        <li key={title} className="rounded-2xl border border-white/10 bg-white/5 p-6">
                            <Icon className="size-6 text-brand" aria-hidden />
                            <h3 className="mt-4 font-display text-lg font-semibold">{title}</h3>
                            <p className="mt-2 text-sm leading-relaxed text-white/70">{body}</p>
                        </li>
                    ))}
                </ul>
                <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
                    <a href={SIGNUP_HREF} className={buttonClasses('primary', 'px-6 py-2.5')}>
                        Join as a coach
                    </a>
                    <Link href="/pricing#coaches" className={buttonClasses('outline-light', 'px-6 py-2.5')}>
                        Coach pricing
                    </Link>
                </div>
            </div>
        </section>
    );
}
