import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ChatInput from '../components/chat/ChatInput.vue'

describe('ChatInput', () => {
    it('renders correctly and is enabled by default', () => {
        const wrapper = mount(ChatInput, {
            props: { isDisabled: false }
        })

        const textarea = wrapper.find('textarea')
        expect(textarea.exists()).toBe(true)
        expect(textarea.attributes('disabled')).toBeUndefined()

        // El botón debería estar disabled si el campo está vacío
        const button = wrapper.find('button')
        expect(button.attributes('disabled')).toBeDefined()
    })

    it('blocks interaction when isDisabled is true', async () => {
        const wrapper = mount(ChatInput, {
            props: { isDisabled: true }
        })

        const textarea = wrapper.find('textarea')
        expect(textarea.attributes('disabled')).toBeDefined()

        const overlay = wrapper.find('.bg-gray-100\\/50') // selector simplificado de Tailwind para el backdrop
        expect(overlay.exists()).toBe(true)
    })

    it('emits "send" event and clears input on valid submit', async () => {
        const wrapper = mount(ChatInput, {
            props: { isDisabled: false }
        })

        const textarea = wrapper.find('textarea')
        await textarea.setValue('Hola Oak!')

        const button = wrapper.find('button')
        // El botón ya no debe estar disabled
        expect(button.attributes('disabled')).toBeUndefined()

        await button.trigger('click')

        // Check Events
        expect(wrapper.emitted('send')).toBeTruthy()
        expect(wrapper.emitted('send')![0]).toEqual(['Hola Oak!'])

        // Check if input was cleared
        expect(textarea.element.value).toBe('')
    })

    it('prevents sending empty blanks', async () => {
        const wrapper = mount(ChatInput, {
            props: { isDisabled: false }
        })

        const textarea = wrapper.find('textarea')
        await textarea.setValue('   ')

        const button = wrapper.find('button')
        expect(button.attributes('disabled')).toBeDefined()
    })
})
