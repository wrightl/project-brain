import { SkeletonList } from '@/_components/ui/skeleton';

export default function UserAppLoading() {
    return (
        <div className="mx-auto max-w-7xl px-4 py-8">
            <SkeletonList count={4} />
        </div>
    );
}
