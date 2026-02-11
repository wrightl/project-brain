import FindCoachesClient from './_components/find-coaches-client';
import { UserService } from '@/_services/user-service';

export default async function FindCoachesPage() {
    const user = await UserService.getCurrentUser();
    return (
        <FindCoachesClient
            defaultCountryName={user?.country || ''}
            userLatitude={user?.latitude}
            userLongitude={user?.longitude}
        />
    );
}

