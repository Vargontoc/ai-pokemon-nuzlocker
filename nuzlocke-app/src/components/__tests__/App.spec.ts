import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import App from '../../App.vue'
import router from '../../router'
import i18n from '../../config/i18n'

describe('App', () => {
    it('renders correctly', () => {
        const wrapper = mount(App, {
            global: {
                plugins: [router, i18n]
            }
        })
        expect(wrapper.exists()).toBe(true)
    })
})
