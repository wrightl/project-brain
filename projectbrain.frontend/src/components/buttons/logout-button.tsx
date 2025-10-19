export const LogoutButton = () => {
    return (
        // eslint-disable-next-line @next/next/no-html-link-for-pages
        <a className="button__logout" href="/api/auth/logout">
            Log Out
        </a>
    );
};
