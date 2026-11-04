import Link from 'next/link';
import { buttonClasses, SIGNUP_HREF } from './links';
import { ChatMock, GoalsMock } from './product-mocks';

export function MarketingHero() {
    return (
        <section className="hero-dots relative overflow-hidden px-6 pt-16 pb-16 text-white md:pt-20 md:pb-24">
            <div className="mx-auto grid max-w-6xl grid-cols-1 items-center gap-12 lg:grid-cols-2 lg:gap-16">
                <div className="text-center lg:text-left">
                    <p className="text-sm tracking-widest text-brand uppercase">
                        ProjectBrain by Dot + Dash
                    </p>
                    <h1 className="mt-4 font-display text-4xl leading-tight font-normal tracking-tight sm:text-5xl md:text-6xl">
                        Run your business the way your brain works.
                    </h1>
                    <p className="mx-auto mt-5 max-w-xl text-lg text-white/75 lg:mx-0">
                        An AI coaching buddy and real coaches for neurodivergent
                        entrepreneurs. Break big jobs into three small goals a
                        day, keep a journal that remembers what helped, and get
                        a human in your corner when you need one.
                    </p>
                    <div className="mt-10 flex flex-wrap items-center justify-center gap-3 lg:justify-start">
                        <a href={SIGNUP_HREF} className={buttonClasses('primary', 'px-6 py-2.5')}>
                            Sign up free
                        </a>
                        <Link href="#product" className={buttonClasses('outline-light', 'px-6 py-2.5')}>
                            See how it works
                        </Link>
                    </div>
                    <p className="mt-4 text-sm text-white/50">
                        Free plan, no card needed.
                    </p>
                </div>
                {/* The goals card tucks under the chat's bottom-left, where the last (right-aligned) reply leaves space. */}
                <div className="relative mx-auto w-full max-w-xl sm:pb-72 lg:max-w-none">
                    <div className="rounded-2xl shadow-2xl shadow-black/40 sm:ml-10">
                        <ChatMock />
                    </div>
                    <div className="mt-4 rounded-2xl shadow-2xl shadow-black/40 sm:absolute sm:bottom-0 sm:left-0 sm:mt-0 sm:w-72">
                        <GoalsMock />
                    </div>
                </div>
            </div>
        </section>
    );
}
