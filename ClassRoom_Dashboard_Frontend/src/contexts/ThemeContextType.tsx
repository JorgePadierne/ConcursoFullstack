// src/context/ThemeContextTypes.ts

// Define los posibles valores del tema
export type Theme = "light" | "dark";

// Define la forma del contexto
export interface ThemeContextType {
  theme: Theme; // el tema actual
  toggleTheme: () => void; // función para alternar el tema
}
