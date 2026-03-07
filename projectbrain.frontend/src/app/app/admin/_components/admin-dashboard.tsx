import Link from 'next/link';
import {
    UsersIcon,
    CloudArrowUpIcon,
    DocumentTextIcon,
    Cog6ToothIcon,
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
        },
        {
            title: 'Manage Knowledge',
            description: 'Manage knowledge base files',
            href: '/app/admin/manage-files',
            icon: CloudArrowUpIcon,
        },
        {
            title: 'Manage Quizzes',
            description: 'Create and manage assessment quizzes',
            href: '/app/admin/quizzes',
            icon: DocumentTextIcon,
        },
    ];

    return (
        <div
            className="min-h-screen rounded-xl border border-gray-300 shadow-lg overflow-hidden"
            style={{ background: 'var(--dashboard-gradient)' }}
        >
            <div className="p-6 md:p-8 lg:px-14 space-y-8">
                {/* Header */}
                <header className="flex flex-wrap items-center justify-between gap-4">
                    <div className="flex items-center gap-3">
                        <div
                            className="w-8 h-8 rounded-md flex-shrink-0"
                            style={{
                                background:
                                    'linear-gradient(135deg, #22c55e, #3b82f6)',
                            }}
                        />
                        <span className="text-white font-bold text-base tracking-wide">
                            ProjectBrain
                        </span>
                    </div>
                    <h1 className="text-2xl md:text-3xl font-bold text-white tracking-tight">
                        Analytics Overview
                    </h1>
                    <div className="flex items-center gap-3">
                        <Link
                            href="/app/admin/users"
                            className="inline-flex items-center gap-2 rounded-md bg-emerald-500 px-4 py-2 text-sm font-medium text-gray-900 hover:bg-emerald-400"
                        >
                            Manage users
                        </Link>
                        <Link
                            href="/app/admin/settings"
                            className="inline-flex items-center gap-2 rounded-md border border-gray-300 bg-white/10 px-4 py-2 text-sm font-medium text-white hover:bg-white/20"
                        >
                            <Cog6ToothIcon className="h-4 w-4" />
                            Settings
                        </Link>
                    </div>
                </header>

                {/* KPI row */}
                <section>
                    <AdminKpiRow totalUsers={allUsersCount} />
                </section>

                {/* Chart + Segments */}
                <section className="grid grid-cols-1 lg:grid-cols-[1fr_280px] gap-6">
                    <AdminEngagementChart />
                    <AdminSegmentsPanel />
                </section>

                {/* Quick actions */}
                <section>
                    <h2 className="text-lg font-semibold text-white mb-4">
                        Quick Actions
                    </h2>
                    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                        {quickActions.map((action) => {
                            const Icon = action.icon;
                            return (
                                <Link
                                    key={action.href}
                                    href={action.href}
                                    className="flex items-center gap-4 rounded-lg p-4 border border-gray-300 text-gray-900 bg-white shadow hover:shadow-md transition-shadow"
                                >
                                    <span className="rounded-lg bg-indigo-500 p-2 text-white">
                                        <Icon className="h-5 w-5" />
                                    </span>
                                    <div>
                                        <p className="font-medium text-gray-900">
                                            {action.title}
                                        </p>
                                        <p className="text-sm text-gray-500">
                                            {action.description}
                                        </p>
                                    </div>
                                </Link>
                            );
                        })}
                    </div>
                </section>

                {/* Collapsible debug pane */}
                <details className="rounded-lg border border-gray-300 bg-white shadow">
                    <summary className="cursor-pointer px-4 py-3 font-medium text-gray-900 hover:bg-gray-50">
                        Debug: Frontend env vars
                    </summary>
                    <div className="px-4 pb-4">
                        <EnvVarsDebugPane />
                    </div>
                </details>
            </div>
        </div>
    );
}
