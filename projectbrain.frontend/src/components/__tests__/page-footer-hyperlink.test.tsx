import { render, screen } from '@testing-library/react'
import { PageFooterHyperlink } from '../page-footer-hyperlink'

describe('PageFooterHyperlink Component', () => {
    it('renders with correct text', () => {
        render(
            <PageFooterHyperlink path="/test">
                Test Link
            </PageFooterHyperlink>
        )

        expect(screen.getByText('Test Link')).toBeInTheDocument()
    })

    it('has correct href attribute', () => {
        render(
            <PageFooterHyperlink path="/about">
                About Us
            </PageFooterHyperlink>
        )

        const link = screen.getByText('About Us')
        expect(link).toHaveAttribute('href', '/about')
    })

    it('opens in new tab when target is specified', () => {
        render(
            <PageFooterHyperlink path="https://example.com" target="_blank">
                External Link
            </PageFooterHyperlink>
        )

        const link = screen.getByText('External Link')
        expect(link).toHaveAttribute('target', '_blank')
    })

    it('has security attributes for external links', () => {
        render(
            <PageFooterHyperlink path="https://example.com" target="_blank">
                External Link
            </PageFooterHyperlink>
        )

        const link = screen.getByText('External Link')
        expect(link).toHaveAttribute('rel', 'noopener noreferrer')
    })

    it('applies hover underline class', () => {
        render(
            <PageFooterHyperlink path="/test">
                Link
            </PageFooterHyperlink>
        )

        const link = screen.getByText('Link')
        expect(link).toHaveClass('hover:underline')
    })
})
