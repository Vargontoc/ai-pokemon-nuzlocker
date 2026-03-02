import api from '../config/api'

// Vite's erasableSyntaxOnly requires value enums to be const objects
export const NuzlockeStatus = {
    Active: 0,
    Completed: 1,
    Failed: 2,
    Archived: 3
} as const;

export type NuzlockeStatusType = typeof NuzlockeStatus[keyof typeof NuzlockeStatus];

export interface NuzlockeSessionInfo {
    id: string
    name: string
    path: string
    createdAt: string
    status?: NuzlockeStatusType
}

export interface CreateSessionRequest {
    name: string
    directoryPath: string
}

export const nuzlockeService = {
    /**
     * Obtiene la lista de todos los Nuzlockes activos.
     */
    async getSessions(): Promise<NuzlockeSessionInfo[]> {
        const response = await api.get<NuzlockeSessionInfo[]>('/nuzlocke/sessions')
        return response.data
    },

    /**
     * Recupera una sesión por su Id único.
     */
    async getSessionById(id: string): Promise<NuzlockeSessionInfo> {
        const response = await api.get<NuzlockeSessionInfo>(`/nuzlocke/sessions/${id}`)
        return response.data
    },

    /**
     * Crea y registra una nueva sesión de juego/nuzlocke.
     */
    async createSession(payload: CreateSessionRequest): Promise<NuzlockeSessionInfo> {
        const response = await api.post<NuzlockeSessionInfo>('/nuzlocke/sessions', payload)
        return response.data
    },

    /**
     * Borra un Nuzlocke activo.
     */
    async deleteSession(id: string): Promise<void> {
        await api.delete(`/nuzlocke/sessions/${id}`)
    }
}
