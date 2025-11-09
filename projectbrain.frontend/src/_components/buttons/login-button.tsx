export const LoginButton = () => {
    return (
        // className="-mx-3 block rounded-lg px-3 py-2.5 text-base/7 font-semibold text-gray-900 hover:bg-gray-50"
        // eslint-disable-next-line @next/next/no-html-link-for-pages
        <a
            className="text-sm/6 font-semibold text-gray-900 dark:text-white"
            href="/auth/login?returnTo=/app"
        >
            Log In <span aria-hidden="true">&rarr;</span>
        </a>
    );
};
