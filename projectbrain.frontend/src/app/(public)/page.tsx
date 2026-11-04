import type { Metadata } from 'next';
import { MarketingHero } from '@/_components/marketing/hero';
import { AudienceStrip } from '@/_components/marketing/audience-strip';
import { ProblemSection } from '@/_components/marketing/problem-section';
import { FeatureSplits } from '@/_components/marketing/feature-splits';
import { MoreFeatures } from '@/_components/marketing/more-features';
import { HowItWorks } from '@/_components/marketing/how-it-works';
import {
    DifferentiatorSection,
    WhoItsFor,
} from '@/_components/marketing/who-its-for';
import { CalmFocusSection } from '@/_components/marketing/calm-focus';
import { ForCoaches } from '@/_components/marketing/for-coaches';
import { FaqSection } from '@/_components/marketing/faq';
import { FinalCta } from '@/_components/marketing/final-cta';

export const metadata: Metadata = {
    title: {
        absolute: 'ProjectBrain — AI coaching for neurodivergent entrepreneurs',
    },
    description:
        'An AI coaching buddy and real coaches for neurodivergent entrepreneurs. Three small goals a day, a journal that remembers what helped, and coaches who understand. Free plan available.',
    openGraph: {
        title: 'ProjectBrain — AI coaching for neurodivergent entrepreneurs',
        description:
            'Run your business the way your brain works. An AI coaching buddy, daily goals, a journal, and real coaches in one calm place.',
    },
};

export default function Home() {
    return (
        <>
            <MarketingHero />
            <AudienceStrip />
            <ProblemSection />
            <FeatureSplits />
            <MoreFeatures />
            <HowItWorks />
            <DifferentiatorSection />
            <CalmFocusSection />
            <ForCoaches />
            <WhoItsFor />
            <FaqSection />
            <FinalCta />
        </>
    );
}
