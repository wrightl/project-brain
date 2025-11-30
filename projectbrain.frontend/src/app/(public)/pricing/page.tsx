import UserTiers from './_components/user-tiers';
import CoachTiers from './_components/coach-tiers';
import PageHeader from '@/_components/page-header';

export default function PricingPage() {
    return (
        <div className="min-h-screen bg-slate-900">
            <PageHeader />
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-32 pb-16">
                <div className="text-center mb-16">
                    <h1 className="text-4xl font-bold mb-4 text-white">Pricing Plans</h1>
                    <p className="text-xl text-gray-300">
                        Choose the plan that's right for you
                    </p>
                </div>

                <div className="space-y-16">
                    <div>
                        <h2 className="text-3xl font-bold text-center mb-8 text-fuchsia-300">
                            For Users
                        </h2>
                        <UserTiers />
                    </div>

                    <div>
                        <h2 className="text-3xl font-bold text-center mb-8 text-fuchsia-300">
                            For Coaches
                        </h2>
                        <CoachTiers />
                    </div>
                </div>
            </div>
        </div>
    );
}
