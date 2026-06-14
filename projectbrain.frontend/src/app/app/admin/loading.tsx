import { SkeletonList } from '@/_components/ui/skeleton';

export default function AdminAppLoading() {
    return (
        <div className="mx-auto max-w-7xl px-4 py-8">
            <SkeletonList count={6} />
        </div>
    );
}
