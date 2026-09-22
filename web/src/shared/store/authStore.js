import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export const useAuthStore = create(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isLoading: false,
      error: null,

      setAuth: (authData) =>
        set({
          accessToken: authData.accessToken,
          refreshToken: authData.refreshToken,
          user: {
            id: authData.userId,
            email: authData.email,
            roles: authData.roles || [],
          },
          error: null,
        }),

      setUser: (user) => set({ user }),
      setToken: (accessToken) => set({ accessToken }),
      setLoading: (isLoading) => set({ isLoading }),
      setError: (error) => set({ error }),

      logout: () =>
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          error: null,
        }),

      hasRole: (role) => {
        const roles = get().user?.roles || [];
        return roles.includes(role.toUpperCase());
      },

      isAdmin: () => {
        const roles = get().user?.roles || [];
        return roles.includes('ADMIN');
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        user: state.user,
      }),
    }
  )
);
