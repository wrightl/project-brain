import { Metadata } from 'next';
import { RoleGuard } from '@/_components/auth/role-guard';
import UserDetailsComponent from './_components/user-details-component';

import { AppRoles } from '@/_lib/roles';
export const metadata: Metadata = {
    title: 'User Details',
    description: 'View and manage user details',
};

export default async function UserDetailsPage({
    params,
}: {
    params: { id: string };
}) {
    return (
        <RoleGuard allowedRoles={[AppRoles.Admin]}>
            <UserDetailsComponent userId={(await params).id} />
        </RoleGuard>
    );
}
