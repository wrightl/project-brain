import { Metadata } from 'next';
import { RoleGuard } from '@/_components/auth/role-guard';
import UserDetailsComponent from './_components/user-details-component';

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
        <RoleGuard allowedRoles={['admin']}>
            <UserDetailsComponent userId={(await params).id} />
        </RoleGuard>
    );
}
