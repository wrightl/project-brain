import dynamicImport from 'next/dynamic';
import { RoleGuard } from '@/_components/auth/role-guard';
import { SkeletonCard } from '@/_components/ui/skeleton';
import { getSession } from '@/_lib/auth';

const ChatInterface = dynamicImport(
    () => import('../_components/chat-interface'),
    {
        loading: () => (
            <div className="h-full w-full flex flex-col bg-gray-50 p-8">
                <SkeletonCard />
            </div>
        ),
    }
);

export default async function StrategiesChatPage() {
    const session = await getSession();
    const displayName =
        session?.user?.name ??
        session?.user?.nickname ??
        session?.user?.email ??
        'there';

    return (
        <RoleGuard allowedRoles={['user']}>
            <div className="h-full w-full flex flex-col bg-gray-50">
                <ChatInterface mode="strategies" displayName={displayName} />
            </div>
        </RoleGuard>
    );
}

