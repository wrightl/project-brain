import { twMerge } from 'tailwind-merge';
import { CheckIcon } from '@heroicons/react/24/outline';
import { buttonClasses, SIGNUP_HREF } from '@/_components/marketing/links';

export type Plan = {
    name: string;
    description: string;
    /** Omit for free plans. */
    monthlyPrice?: string;
    annualPrice?: string;
    annualTotal?: string;
    features: string[];
    cta: string;
    featured?: boolean;
};

export function PlanCards({ plans }: { plans: Plan[] }) {
    return (
        <div
            className={twMerge(
                'mt-12 grid gap-6 md:items-stretch',
                plans.length === 3 ? 'md:grid-cols-3' : 'mx-auto max-w-4xl md:grid-cols-2'
            )}
        >
            {plans.map((plan) => (
                <article
                    key={plan.name}
                    className={twMerge(
                        'flex flex-col rounded-2xl border bg-surface p-6 md:p-8',
                        plan.featured
                            ? 'border-navy shadow-lg ring-2 shadow-navy/10 ring-navy md:-my-2 md:py-10'
                            : 'border-line'
                    )}
                >
                    <p
                        className={twMerge(
                            'mb-3 text-xs font-medium tracking-widest uppercase',
                            plan.featured ? 'text-navy' : 'text-transparent select-none'
                        )}
                        aria-hidden={!plan.featured}
                    >
                        {plan.featured ? 'Most popular' : ' '}
                    </p>
                    <h3 className="font-display text-2xl font-semibold">{plan.name}</h3>
                    <div className="mt-4">
                        {plan.monthlyPrice ? (
                            <>
                                <span className="font-display text-4xl font-normal tracking-tight">
                                    {plan.monthlyPrice}
                                </span>
                                <span className="text-muted"> / month</span>
                                <p className="mt-1 text-sm text-muted">
                                    or {plan.annualPrice} paid annually ({plan.annualTotal})
                                </p>
                            </>
                        ) : (
                            <>
                                <span className="font-display text-4xl font-normal tracking-tight">
                                    $0
                                </span>
                                <p className="mt-1 text-sm text-muted">Free forever, no card needed</p>
                            </>
                        )}
                    </div>
                    <p className="mt-4 text-sm leading-relaxed text-muted">{plan.description}</p>
                    <ul className="mt-6 flex-1 space-y-3">
                        {plan.features.map((feature) => (
                            <li key={feature} className="flex gap-3 text-sm">
                                <CheckIcon className="mt-0.5 size-4 shrink-0 text-emerald-700" aria-hidden />
                                <span>{feature}</span>
                            </li>
                        ))}
                    </ul>
                    <a
                        href={SIGNUP_HREF}
                        className={buttonClasses(
                            plan.featured ? 'primary' : 'secondary',
                            'mt-8 w-full px-6 py-2.5'
                        )}
                    >
                        {plan.cta}
                    </a>
                </article>
            ))}
        </div>
    );
}
