import Image from 'next/image';

/** Dot + Dash mark plus the ProjectBrain wordmark. Wordmark colour inherits from the parent. */
export function Logo({
    size = 40,
    priority = false,
}: {
    size?: number;
    priority?: boolean;
}) {
    return (
        <span className="inline-flex items-center gap-3">
            <Image
                src="/dotdash.png"
                alt=""
                width={size}
                height={size}
                className="shrink-0 rounded-md"
                priority={priority}
            />
            <span className="flex flex-col leading-tight">
                <span className="font-display text-lg">ProjectBrain</span>
                <span className="mt-0.5 text-xs tracking-widest uppercase opacity-80">
                    by Dot + Dash
                </span>
            </span>
        </span>
    );
}
