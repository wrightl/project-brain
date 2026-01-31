import { getSession } from '@/_lib/auth';
import WelcomeSection from './welcome-section';
import TodaysGoalsSection from './todays-goals-section';
import CopingStrategiesSection from './coping-strategies-section';
import NetworkSection from './network-section';
import AchievementsSection from './achievements-section';
import JournalSummarySection from './journal-summary-section';

export default async function UserDashboard() {
    const session = await getSession();
    const displayName =
        session?.user?.name ??
        session?.user?.nickname ??
        session?.user?.email ??
        'there';

    return (
        <div className="space-y-10">
            <WelcomeSection displayName={displayName} />

            <div className="grid grid-cols-1 gap-8 lg:grid-cols-2">
                <TodaysGoalsSection />
                <CopingStrategiesSection />
                <JournalSummarySection />
            </div>

            <NetworkSection />

            <AchievementsSection />
        </div>
    );
}
