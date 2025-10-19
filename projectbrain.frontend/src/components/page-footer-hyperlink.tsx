import React, { PropsWithChildren } from 'react';

interface FooterHyperlinkProps {
    path: string;
    target?: string;
}

export const PageFooterHyperlink: React.FC<
    PropsWithChildren<FooterHyperlinkProps>
> = ({ children, path, target }) => {
    return (
        <a
            className="hover:underline"
            href={path}
            target={target}
            rel="noopener noreferrer"
        >
            {children}
        </a>
    );
};
