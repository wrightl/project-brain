import Link from 'next/link';
import {
    UsersIcon,
    CloudArrowUpIcon,
    DocumentTextIcon,
    Cog6ToothIcon,
    ChartBarIcon,
} from '@heroicons/react/24/outline';
import { StatisticsService } from '@/_services/statistics-service';
import { AdminKpiRow } from './admin-kpi-row';
import { AdminEngagementChart } from './admin-engagement-chart';
import { AdminSegmentsPanel } from './admin-segments-panel';
import EnvVarsDebugPane from './env-vars-debug-pane';

export default async function AdminDashboard() {
    const allUsersCount = await StatisticsService.getAllUsers();

    const quickActions = [
        {
            title: 'Manage Users',
            description: 'View and manage all users and coaches',
            href: '/app/admin/users',
            icon: UsersIcon,
            iconColor: 'text-[color:var(--indigo)]',
        },
        {
            title: 'Manage Knowledge',
            description: 'Manage knowledge base files',
            href: '/app/admin/manage-files',
            icon: CloudArrowUpIcon,
            iconColor: 'text-[color:var(--emerald)]',
        },
        {
            title: 'Manage Quizzes',
            description: 'Create and manage assessment quizzes',
            href: '/app/admin/quizzes',
            icon: DocumentTextIcon,
            iconColor: 'text-[color:var(--aqua)]',
        },
    ];

    return (
        <div className="space-y-8">
            <header className="relative overflow-hidden rounded-lg bg-white p-6 shadow border border-gray-300">
                <div
                    aria-hidden="true"
                    className="pointer-events-none absolute inset-0 opacity-[0.06]"
                    style={{ background: 'var(--indigo-aqua-gradient)' }}
                />
                <div className="relative flex flex-wrap items-start justify-between gap-4">
                    <div className="flex items-start gap-4 min-w-0">
                        <div className="mt-0.5 flex h-10 w-10 items-center justify-center rounded-md bg-[color:var(--light-aluminium)]">
                            <ChartBarIcon className="h-5 w-5 text-[color:var(--indigo)]" />
                        </div>
                        <div className="min-w-0">
                            <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">
                                Analytics Overview
                            </h1>
                            <p className="mt-2 text-sm text-gray-600">
                                Monitor engagement, users, and system activity.
                            </p>
                        </div>
                    </div>
                    <div className="flex items-center gap-3">
                        <Link
                            href="/app/admin/users"
                            className="inline-flex items-center gap-2 rounded-md bg-[color:var(--indigo)] px-4 py-2 text-sm font-medium text-white hover:opacity-90"
                        >
                            Manage users
                        </Link>
                        <Link
                            href="/app/admin/settings"
                            className="inline-flex items-center gap-2 rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-900 hover:bg-gray-100"
                        >
                            <Cog6ToothIcon className="h-4 w-4" />
                            Settings
                        </Link>
                    </div>
                </div>
            </header>

            <section>
                <AdminKpiRow totalUsers={allUsersCount} />
            </section>

            <section className="grid grid-cols-1 lg:grid-cols-[1fr_280px] gap-6">
                <AdminEngagementChart />
                <AdminSegmentsPanel />
            </section>

            <section className="space-y-4">
                <h2 className="text-lg font-semibold text-gray-900">
                    Quick Actions
                </h2>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                    {quickActions.map((action) => {
                        const Icon = action.icon;
                        return (
                            <Link
                                key={action.href}
                                href={action.href}
                                className="flex items-center gap-4 rounded-lg border border-gray-300 bg-white p-4 shadow hover:bg-gray-100 transition-colors"
                            >
                                <span className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-md bg-[color:var(--light-aluminium)]">
                                    <Icon
                                        className={`h-5 w-5 ${action.iconColor}`}
                                    />
                                </span>
                                <div>
                                    <p className="font-medium text-gray-900">
                                        {action.title}
                                    </p>
                                    <p className="text-sm text-gray-600">
                                        {action.description}
                                    </p>
                                </div>
                            </Link>
                        );
                    })}
                </div>
            </section>

            <EnvVarsDebugPane />
        </div>
    );
}
