import { render, screen } from '@testing-library/react'
import { Column } from '../column'
import { CardProps } from '../card'

describe('Column Component', () => {
    const mockCards: CardProps[] = [
        { title: 'Card 1' },
        { title: 'Card 2' },
    ]

    it('renders with title', () => {
        render(
            <Column title="Test Column" cards={mockCards} />
        )

        expect(screen.getByText('Test Column')).toBeInTheDocument()
    })

    it('displays correct task count', () => {
        render(
            <Column title="Test Column" cards={mockCards} />
        )

        expect(screen.getByText('2 tasks')).toBeInTheDocument()
    })

    it('renders all cards', () => {
        render(
            <Column title="Test Column" cards={mockCards} />
        )

        expect(screen.getByText('Card 1')).toBeInTheDocument()
        expect(screen.getByText('Card 2')).toBeInTheDocument()
    })

    it('displays zero tasks when no cards provided', () => {
        render(
            <Column title="Empty Column" cards={[]} />
        )

        expect(screen.getByText('0 tasks')).toBeInTheDocument()
    })

    it('renders multiple cards correctly', () => {
        const manyCards: CardProps[] = [
            { title: 'Task 1' },
            { title: 'Task 2' },
            { title: 'Task 3' },
        ]

        render(
            <Column title="Busy Column" cards={manyCards} />
        )

        expect(screen.getByText('3 tasks')).toBeInTheDocument()
        expect(screen.getByText('Task 1')).toBeInTheDocument()
        expect(screen.getByText('Task 2')).toBeInTheDocument()
        expect(screen.getByText('Task 3')).toBeInTheDocument()
    })
})
