/** @type {import('tailwindcss').Config} */
export default {
  darkMode: ["attribute", "data-theme"], // ⚠️ IMPORTANTE
  content: ["./index.html", "./src/**/*.{ts,tsx,js,jsx}"],
  theme: {
    extend: {},
  },
  plugins: [],
};
