import { createContext } from "react";
import type { ThemeContextType } from "./ThemeContextType"; // opcional

export const ThemeContext = createContext<ThemeContextType | undefined>(
  undefined
);
