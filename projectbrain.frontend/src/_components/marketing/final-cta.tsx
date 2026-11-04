import { buttonClasses, LOGIN_HREF, SIGNUP_HREF } from './links';

export function FinalCta({
    heading = 'Run your business the way your brain works.',
    body = 'Sign up free and set up your coaching buddy in a few minutes. Bring in a coach whenever you’re ready.',
    primaryLabel = 'Sign up free',
}: {
    heading?: string;
    body?: string;
    primaryLabel?: string;
}) {
    return (
        <section className="hero-dots px-6 py-20 text-center text-white md:py-24">
            <div className="mx-auto max-w-3xl">
                <h2 className="font-display text-3xl font-normal tracking-tight md:text-5xl">
                    {heading}
                </h2>
                <p className="mx-auto mt-5 max-w-xl text-lg text-white/75">{body}</p>
                <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
                    <a href={SIGNUP_HREF} className={buttonClasses('primary', 'px-6 py-2.5')}>
                        {primaryLabel}
                    </a>
                    <a href={LOGIN_HREF} className={buttonClasses('outline-light', 'px-6 py-2.5')}>
                        Log in
                    </a>
                </div>
            </div>
        </section>
    );
}
