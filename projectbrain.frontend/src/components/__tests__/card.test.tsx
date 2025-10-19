import { render, screen } from '@testing-library/react'
import { Card } from '../card'

describe('Card Component', () => {
    it('renders with title and children', () => {
        render(
            <Card title="Test Card">
                Test content
            </Card>
        )

        expect(screen.getByText('Test Card')).toBeInTheDocument()
        expect(screen.getByText('Test content')).toBeInTheDocument()
    })

    it('renders without title', () => {
        render(
            <Card>
                Just content
            </Card>
        )

        expect(screen.getByText('Just content')).toBeInTheDocument()
    })

    it('renders with task badge', () => {
        render(
            <Card title="Test">
                Content
            </Card>
        )

        expect(screen.getByText('Task')).toBeInTheDocument()
    })

    it('applies correct styling classes', () => {
        const { container } = render(
            <Card title="Test">
                Content
            </Card>
        )

        const cardDiv = container.firstChild as HTMLElement
        expect(cardDiv).toHaveClass('mb-4', 'bg-white', 'rounded-lg', 'border', 'border-gray-200', 'p-4')
    })

    it('renders drag handle emoji', () => {
        render(
            <Card title="Test">
                Content
            </Card>
        )

        expect(screen.getByText('↕️')).toBeInTheDocument()
    })
})
