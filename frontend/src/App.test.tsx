import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from './App'

describe('App', () => {
    it('should render the app', () => {
        render(<App />)
        expect(screen.getByText('Frontend App')).toBeInTheDocument()
    })

    it('should increment count when button is clicked', async () => {
        const user = userEvent.setup()
        render(<App />)

        const button = screen.getByRole('button', { name: /count is/i })
        expect(button).toHaveTextContent('count is 0')

        await user.click(button)
        // Исправленный тест
        expect(button).toHaveTextContent('count is 1')
    })
})

