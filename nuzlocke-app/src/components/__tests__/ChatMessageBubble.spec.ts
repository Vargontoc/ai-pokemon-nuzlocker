import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ChatMessageBubble from '../chat/ChatMessageBubble.vue'
import type { ChatMessage } from '../../composables/useNuzlockeSocket'

describe('ChatMessageBubble', () => {
    it('renders standard text conversation correctly for AI', () => {
        const msg: ChatMessage = {
            id: '1',
            sender: 'ai',
            type: 'message',
            content: 'Hola, soy Oak',
            timestamp: new Date()
        }
        const wrapper = mount(ChatMessageBubble, { props: { msg } })

        // Debería tener el texto
        expect(wrapper.text()).toContain('Hola, soy Oak')
        // No debería tener el chip de ToolCall ni el recuadro de Event
        expect(wrapper.html()).not.toContain('Consultando')
        expect(wrapper.html()).not.toContain('Evento Nuzlocke')
    })

    it('renders agent_tool_call as a loading chip', () => {
        const msg: ChatMessage = {
            id: '2',
            sender: 'system',
            type: 'agent_tool_call',
            toolName: 'pokeapi',
            content: '',
            timestamp: new Date()
        }
        const wrapper = mount(ChatMessageBubble, { props: { msg } })

        expect(wrapper.text()).toContain('Consultando pokeapi...')
        expect(wrapper.find('svg.animate-spin').exists()).toBe(true)
    })

    it('renders workflow_event with custom styling based on eventId', () => {
        const msg: ChatMessage = {
            id: '3',
            sender: 'system',
            type: 'workflow_event',
            eventId: 'pokemon_caught',
            content: 'Atrapaste un Pidgey',
            stateChanges: {
                'Pidgey Level': '5',
                'Pokeballs': '-1'
            },
            timestamp: new Date()
        }
        const wrapper = mount(ChatMessageBubble, { props: { msg } })

        // Verifica clases de estilo (green para capture)
        expect(wrapper.html()).toContain('bg-green-50')
        expect(wrapper.text()).toContain('POKEMON CAUGHT')
        expect(wrapper.text()).toContain('Atrapaste un Pidgey')

        // Verifica rendering de StateChanges
        expect(wrapper.text()).toContain('Pidgey Level')
        expect(wrapper.text()).toContain('5')
        expect(wrapper.text()).toContain('Pokeballs')
        expect(wrapper.text()).toContain('-1')
    })
})
