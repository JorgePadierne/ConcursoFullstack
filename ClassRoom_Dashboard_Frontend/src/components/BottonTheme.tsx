import { useTheme } from "../hooks/useTheme";
import { SunIcon, MoonIcon } from "@heroicons/react/24/outline";

export const ThemeToggleButton = () => {
  const { theme, toggleTheme } = useTheme();

  return (
    <button
      onClick={toggleTheme}
      className="flex items-center gap-2 px-4 py-2 bg-gray-200 dark:bg-gray-700 text-black dark:text-white rounded transition-colors duration-300"
    >
      {theme === "light" ? (
        <MoonIcon className="h-5 w-5" />
      ) : (
        <SunIcon className="h-5 w-5" />
      )}
      {theme === "light" ? "oscuro" : "claro"}
    </button>
  );
};
