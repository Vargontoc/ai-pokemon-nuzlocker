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
    status: number
    description?: string | null
    createdAt: string
    updatedAt: string
}

export interface InventoryItem {
    name: string
    quantity: number
}

export interface StoredPokemon {
    nickname?: string | null
    species: string
    level: number
}

export interface DeadPokemon {
    nickname?: string | null
    species: string
    level: number
    causeOfDeath?: string | null
}

export interface PokemonStats {
    hp: number
    attack: number
    defense: number
    spAttack: number
    spDefense: number
    speed: number
    hpMax?: number // Añadido opcional de lógica Front si hiciera falta
    statExp?: number[]
}

export interface TeamMember {
    nickname?: string | null
    species: string
    level: number
    status?: string | null
    stats?: PokemonStats
}

export interface CreateSessionRequest {
    name: string
    directoryPath: string
}

export interface AdviceRequest {
    question?: string | null
    nuzlockeId?: string | null
    language?: string | null
}

export const nuzlockeService = {
    /**
     * Obtiene la lista de todos los Nuzlockes activos.
     */
    async getSessions(): Promise<NuzlockeSessionInfo[]> {
        const response = await api.get<NuzlockeSessionInfo[]>('/nuzlocke')
        return response.data
    },

    /**
     * Recupera una sesión por su Id único.
     */
    async getSessionById(id: string): Promise<NuzlockeSessionInfo> {
        const response = await api.get<NuzlockeSessionInfo>(`/nuzlocke/${id}`)
        return response.data
    },

    async updateSession(id: string, name: string, description?: string): Promise<NuzlockeSessionInfo> {
        const response = await api.put<NuzlockeSessionInfo>(`/nuzlocke/${id}`, { name, description })
        return response.data
    },

    async getInventory(id: string): Promise<InventoryItem[]> {
        const response = await api.get<InventoryItem[]>(`/nuzlocke/${id}/inventory`)
        return response.data
    },

    async getPC(id: string): Promise<StoredPokemon[]> {
        const response = await api.get<StoredPokemon[]>(`/nuzlocke/${id}/pc`)
        return response.data
    },

    async getGraveyard(id: string): Promise<DeadPokemon[]> {
        const response = await api.get<DeadPokemon[]>(`/nuzlocke/${id}/graveyard`)
        return response.data
    },

    async getActiveParty(id: string): Promise<TeamMember[]> {
        const response = await api.get<TeamMember[]>(`/nuzlocke/${id}/party`)
        return response.data
    },

    /**
     * Crea y registra una nueva sesión de juego/nuzlocke.
     */
    async createSession(payload: CreateSessionRequest): Promise<NuzlockeSessionInfo> {
        const response = await api.post<NuzlockeSessionInfo>('/nuzlocke', payload)
        return response.data
    },

    /**
     * Borra un Nuzlocke activo.
     */
    async deleteSession(id: string): Promise<void> {
        await api.delete(`/nuzlocke/${id}`)
    },

    /**
     * Envía un mensaje o prompt del usuario a la Inteligencia Artificial.
     */
    async askAdvice(payload: AdviceRequest): Promise<void> {
        await api.post('/nuzlocke/advice', payload)
    }
}
