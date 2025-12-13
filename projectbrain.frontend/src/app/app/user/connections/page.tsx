import { redirect } from 'next/navigation';
import { UserService } from '@/_services/user-service';
import ConnectionsPageContent from './_components/connections-page-content';

export const dynamic = 'force-dynamic';

export default async function ConnectionsPage() {
    const user = await UserService.getCurrentUser();
    if (!user) {
        redirect('/auth/login?returnTo=/app/user/connections');
    }

    return <ConnectionsPageContent />;
}

