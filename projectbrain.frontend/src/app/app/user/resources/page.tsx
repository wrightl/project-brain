import { redirect } from 'next/navigation';
import { UserService } from '@/_services/user-service';
import ResourcesPageContent from './_components/resources-page-content';

export const dynamic = 'force-dynamic';

export default async function ResourcesPage() {
    const user = await UserService.getCurrentUser();
    if (!user) {
        redirect('/auth/login?returnTo=/app/user/resources');
    }

    return <ResourcesPageContent />;
}

