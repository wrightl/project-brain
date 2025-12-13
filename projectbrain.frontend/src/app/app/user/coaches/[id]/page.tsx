import { notFound } from 'next/navigation';
import { CoachService } from '@/_services/coach-service';
import CoachDetailView from './_components/coach-detail-view';

interface CoachPageProps {
    params: Promise<{ id: string }>;
}

export default async function CoachPage({ params }: CoachPageProps) {
    const { id } = await params;
    const coach = await CoachService.getCoachById(parseInt(id));

    if (!coach) {
        notFound();
    }

    return <CoachDetailView coach={coach} />;
}
