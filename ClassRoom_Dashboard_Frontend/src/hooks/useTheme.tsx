import { useContext } from "react";
import { ThemeContext } from "../contexts/ThemeContextObject";
import type { ThemeContextType } from "../contexts/ThemeContextType";

export const useTheme = (): ThemeContextType => {
  const context = useContext(ThemeContext);
  if (!context) throw new Error("useTheme debe usarse dentro de ThemeProvider");
  return context;
};
